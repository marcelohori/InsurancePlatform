using Analise.Application.Dtos;
using Analise.Application.Ports;

namespace Analise.Application.UseCases;

public sealed class ConsultarAnalisePorPropostaUseCase(IAnaliseRepository repositorio)
{
    public async Task<AnaliseDto?> ExecutarAsync(Guid propostaId, CancellationToken cancellationToken)
    {
        var analise = await repositorio.ObterPorPropostaIdAsync(propostaId, cancellationToken);
        return analise is null ? null : AnaliseDto.DeEntidade(analise);
    }
}
