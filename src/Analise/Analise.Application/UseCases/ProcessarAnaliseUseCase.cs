using Analise.Application.Exceptions;
using Analise.Application.Ports;
using Analise.Domain;

namespace Analise.Application.UseCases;

/// <summary>
/// Triggered by the inbound adapter when a PropostaCriadaEvent is consumed. Starts (or skips,
/// if already started for this proposal) a risk assessment, and resolves it to Concluida or
/// Falha - a failure never propagates back to the caller, so a flaky AI provider can never
/// block the proposal flow (see spec "Falha não bloqueia o fluxo de proposta").
/// </summary>
public sealed class ProcessarAnaliseUseCase(
    IAnaliseRepository repositorio,
    IRiskAssessmentPort riskAssessmentPort,
    IUnitOfWork unitOfWork,
    TimeProvider? timeProvider = null)
{
    private static readonly TimeSpan TempoLimiteProcessamento = TimeSpan.FromMinutes(15);

    public async Task ExecutarAsync(RiskAssessmentInput input, CancellationToken cancellationToken)
    {
        var agora = (timeProvider ?? TimeProvider.System).GetUtcNow();
        var existente = await repositorio.ObterPorPropostaIdAsync(input.PropostaId, cancellationToken);
        if (existente is not null)
        {
            if (existente.Status == StatusAnalise.Concluida)
            {
                return;
            }

            var processamentoExpirado = existente.Status == StatusAnalise.EmProcessamento
                && agora - existente.DataCriacao >= TempoLimiteProcessamento;

            if (existente.Status != StatusAnalise.Falha && !processamentoExpirado)
            {
                return;
            }

            if (existente.Status == StatusAnalise.Falha)
            {
                existente.Reiniciar();
            }
            else
            {
                existente.ReiniciarQuandoExpirada();
            }
            await unitOfWork.SalvarAlteracoesAsync(cancellationToken);
        }
        else
        {
            existente = AnaliseRisco.Iniciar(input.PropostaId, timeProvider);
            repositorio.Adicionar(existente);
            await unitOfWork.SalvarAlteracoesAsync(cancellationToken);
        }

        try
        {
            var resultado = await riskAssessmentPort.AvaliarAsync(input, cancellationToken);
            existente.ConcluirComSucesso(resultado.ScoreRisco, resultado.Recomendacao, resultado.Justificativa, timeProvider);
        }
        catch (Exception ex)
        {
            existente.ConcluirComFalha(
                ex is RiskAssessmentIndisponivelException
                    ? ex.Message
                    : "Não foi possível concluir a análise de risco.",
                timeProvider);
        }

        repositorio.Atualizar(existente);
        await unitOfWork.SalvarAlteracoesAsync(cancellationToken);
    }
}
