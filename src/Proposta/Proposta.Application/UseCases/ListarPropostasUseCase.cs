using Proposta.Application.Dtos;
using Proposta.Domain.Exceptions;
using Proposta.Application.Ports;

namespace Proposta.Application.UseCases;

public sealed class ListarPropostasUseCase(IPropostaRepository repositorio)
{
    private const int TamanhoPaginaMaximo = 100;

    public Task<IReadOnlyList<PropostaDto>> ExecutarAsync(int pagina, int tamanhoPagina, CancellationToken cancellationToken) =>
        ExecutarAsync(pagina, tamanhoPagina, null, cancellationToken);

    public async Task<IReadOnlyList<PropostaDto>> ExecutarAsync(int pagina, int tamanhoPagina, string? criadoPor, CancellationToken cancellationToken)
    {
        ValidarPaginacao(pagina, tamanhoPagina);
        var propostas = await repositorio.ListarAsync(pagina, tamanhoPagina, criadoPor, cancellationToken);
        return [.. propostas.Select(PropostaDto.DeEntidade)];
    }

    private static void ValidarPaginacao(int pagina, int tamanhoPagina)
    {
        if (pagina < 1)
        {
            throw new DomainException("A página deve ser maior ou igual a 1.");
        }

        if (tamanhoPagina is < 1 or > TamanhoPaginaMaximo)
        {
            throw new DomainException($"O tamanho da página deve estar entre 1 e {TamanhoPaginaMaximo}.");
        }
    }
}
