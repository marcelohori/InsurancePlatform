using Analise.Application.Ports;
using Analise.Application.UseCases;
using Analise.Domain;
using Xunit;

namespace Analise.UnitTests.UseCases;

public class ProcessarAnaliseUseCaseTests
{
    private static RiskAssessmentInput Entrada(Guid propostaId) => new(propostaId, "Auto", 50000m, 1200m);

    [Fact]
    public async Task Executar_Com_Sucesso_Conclui_Analise_Com_Score_E_Recomendacao()
    {
        var repositorio = new FakeAnaliseRepository();
        var port = FakeRiskAssessmentPort.ComSucesso(new RiskAssessmentResult(72, Recomendacao.Aprovar, "Risco moderado."));
        var useCase = new ProcessarAnaliseUseCase(repositorio, port, new FakeUnitOfWork(), Microsoft.Extensions.Logging.Abstractions.NullLogger<ProcessarAnaliseUseCase>.Instance);
        var propostaId = Guid.NewGuid();

        await useCase.ExecutarAsync(Entrada(propostaId), CancellationToken.None);

        var analise = await repositorio.ObterPorPropostaIdAsync(propostaId, CancellationToken.None);
        Assert.NotNull(analise);
        Assert.Equal(StatusAnalise.Concluida, analise!.Status);
        Assert.Equal(72, analise.ScoreRisco);
        Assert.Equal(Recomendacao.Aprovar, analise.Recomendacao);
    }

    [Fact]
    public async Task Executar_Com_Provedor_De_IA_Indisponivel_Marca_Analise_Como_Falha_Sem_Lancar_Excecao()
    {
        var repositorio = new FakeAnaliseRepository();
        var port = FakeRiskAssessmentPort.ComFalha("Provedor de IA indisponÃ­vel.");
        var useCase = new ProcessarAnaliseUseCase(repositorio, port, new FakeUnitOfWork(), Microsoft.Extensions.Logging.Abstractions.NullLogger<ProcessarAnaliseUseCase>.Instance);
        var propostaId = Guid.NewGuid();

        await useCase.ExecutarAsync(Entrada(propostaId), CancellationToken.None);

        var analise = await repositorio.ObterPorPropostaIdAsync(propostaId, CancellationToken.None);
        Assert.NotNull(analise);
        Assert.Equal(StatusAnalise.Falha, analise!.Status);
        Assert.Null(analise.ScoreRisco);
    }

    [Fact]
    public async Task Executar_Duas_Vezes_Para_A_Mesma_Proposta_Processa_Apenas_Uma_Vez()
    {
        var repositorio = new FakeAnaliseRepository();
        var port = FakeRiskAssessmentPort.ComSucesso(new RiskAssessmentResult(50, Recomendacao.Aprovar, "x"));
        var useCase = new ProcessarAnaliseUseCase(repositorio, port, new FakeUnitOfWork(), Microsoft.Extensions.Logging.Abstractions.NullLogger<ProcessarAnaliseUseCase>.Instance);
        var propostaId = Guid.NewGuid();

        await useCase.ExecutarAsync(Entrada(propostaId), CancellationToken.None);
        var primeiraAnalise = await repositorio.ObterPorPropostaIdAsync(propostaId, CancellationToken.None);

        await useCase.ExecutarAsync(Entrada(propostaId), CancellationToken.None);
        var analiseAposSegundaChamada = await repositorio.ObterPorPropostaIdAsync(propostaId, CancellationToken.None);

        Assert.Equal(primeiraAnalise!.Id, analiseAposSegundaChamada!.Id);
    }

    [Fact]
    public async Task Executar_Com_Analise_Em_Falha_Reprocessa_E_Conclui()
    {
        var repositorio = new FakeAnaliseRepository();
        var propostaId = Guid.NewGuid();
        var analise = AnaliseRisco.Iniciar(propostaId);
        analise.ConcluirComFalha("Falha temporária.");
        repositorio.Adicionar(analise);

        var port = FakeRiskAssessmentPort.ComSucesso(new RiskAssessmentResult(40, Recomendacao.Aprovar, "Reprocessada."));
        var useCase = new ProcessarAnaliseUseCase(repositorio, port, new FakeUnitOfWork(), Microsoft.Extensions.Logging.Abstractions.NullLogger<ProcessarAnaliseUseCase>.Instance);

        await useCase.ExecutarAsync(Entrada(propostaId), CancellationToken.None);

        var resultado = await repositorio.ObterPorPropostaIdAsync(propostaId, CancellationToken.None);
        Assert.Equal(StatusAnalise.Concluida, resultado!.Status);
        Assert.Equal(40, resultado.ScoreRisco);
    }

    [Fact]
    public async Task Executar_Com_Analise_Em_Processamento_Ha_Mais_De_15_Minutos_Reprocessa()
    {
        var repositorio = new FakeAnaliseRepository();
        var propostaId = Guid.NewGuid();
        var analise = AnaliseRisco.Reidratar(
            Guid.NewGuid(),
            propostaId,
            StatusAnalise.EmProcessamento,
            null,
            null,
            null,
            DateTimeOffset.UtcNow.AddMinutes(-16),
            null);
        repositorio.Adicionar(analise);

        var port = FakeRiskAssessmentPort.ComSucesso(new RiskAssessmentResult(35, Recomendacao.Rejeitar, "Retomada."));
        var useCase = new ProcessarAnaliseUseCase(repositorio, port, new FakeUnitOfWork(), Microsoft.Extensions.Logging.Abstractions.NullLogger<ProcessarAnaliseUseCase>.Instance);

        await useCase.ExecutarAsync(Entrada(propostaId), CancellationToken.None);

        var resultado = await repositorio.ObterPorPropostaIdAsync(propostaId, CancellationToken.None);
        Assert.Equal(StatusAnalise.Concluida, resultado!.Status);
        Assert.Equal(35, resultado.ScoreRisco);
    }
}

