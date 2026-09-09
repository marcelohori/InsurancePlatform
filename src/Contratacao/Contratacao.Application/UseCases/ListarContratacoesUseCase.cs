using Contratacao.Application.Dtos;
using Contratacao.Application.Ports;
using Contratacao.Domain.Exceptions;

namespace Contratacao.Application.UseCases;

public sealed class ListarContratacoesUseCase(IContratacaoRepository repositorio)
{
    private const int TamanhoPaginaMaximo = 100;

    public async Task<IReadOnlyList<ContratacaoDto>> ExecutarAsync(int pagina, int tamanhoPagina, CancellationToken cancellationToken)
    {
        if (pagina < 1)
        {
            throw new DomainException("A página deve ser maior ou igual a 1.");
        }

        if (tamanhoPagina is < 1 or > TamanhoPaginaMaximo)
        {
            throw new DomainException($"O tamanho da página deve estar entre 1 e {TamanhoPaginaMaximo}.");
        }

        var apolices = await repositorio.ListarAsync(pagina, tamanhoPagina, cancellationToken);
        return [.. apolices.Select(ContratacaoDto.DeEntidade)];
    }
}
