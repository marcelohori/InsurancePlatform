using Contratacao.Domain;

namespace Contratacao.Application.Ports;

public interface IContratacaoRepository
{
    void Adicionar(ApoliceSeguro apolice);

    Task<ApoliceSeguro?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<ApoliceSeguro>> ListarAsync(int pagina, int tamanhoPagina, CancellationToken cancellationToken);

    void Atualizar(ApoliceSeguro apolice);

    void Remover(ApoliceSeguro apolice);
}
