using Proposta.Application.Dtos;
using Proposta.Application.Ports;

namespace Proposta.Application.UseCases;

public sealed class ObterPropostaPorIdUseCase(IPropostaRepository repositorio)
{
    public Task<PropostaDto?> ExecutarAsync(Guid id, CancellationToken cancellationToken) =>
        ExecutarAsync(id, null, cancellationToken);

    public async Task<PropostaDto?> ExecutarAsync(Guid id, string? criadoPor, CancellationToken cancellationToken)
    {
        var proposta = await repositorio.ObterPorIdAsync(id, criadoPor, cancellationToken);
        return proposta is null ? null : PropostaDto.DeEntidade(proposta);
    }
}
