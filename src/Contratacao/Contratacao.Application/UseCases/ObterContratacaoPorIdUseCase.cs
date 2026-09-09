using Contratacao.Application.Dtos;
using Contratacao.Application.Ports;

namespace Contratacao.Application.UseCases;

public sealed class ObterContratacaoPorIdUseCase(IContratacaoRepository repositorio)
{
    public async Task<ContratacaoDto?> ExecutarAsync(Guid id, CancellationToken cancellationToken)
    {
        var apolice = await repositorio.ObterPorIdAsync(id, cancellationToken);
        return apolice is null ? null : ContratacaoDto.DeEntidade(apolice);
    }
}
