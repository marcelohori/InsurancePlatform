using Proposta.Domain;

namespace Proposta.Application.Ports;

/// <summary>
/// Outbound port for tracking changes to Proposta aggregates. Mutating methods only stage
/// the change - <see cref="IUnitOfWork.SalvarAlteracoesAsync"/> commits it, in the same
/// transaction as any event staged via <see cref="IEventPublisher"/> (transactional outbox).
/// </summary>
public interface IPropostaRepository
{
    void Adicionar(PropostaSeguro proposta);

    Task<PropostaSeguro?> ObterPorIdAsync(Guid id, string? criadoPor, CancellationToken cancellationToken);

    Task<PropostaSeguro?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken) =>
        ObterPorIdAsync(id, null, cancellationToken);

    Task<IReadOnlyList<PropostaSeguro>> ListarAsync(int pagina, int tamanhoPagina, string? criadoPor, CancellationToken cancellationToken);

    Task<IReadOnlyList<PropostaSeguro>> ListarAsync(int pagina, int tamanhoPagina, CancellationToken cancellationToken) =>
        ListarAsync(pagina, tamanhoPagina, null, cancellationToken);

    void Atualizar(PropostaSeguro proposta);

    void Remover(PropostaSeguro proposta);
}
