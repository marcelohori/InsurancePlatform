namespace Proposta.Application.Ports;

/// <summary>
/// Commits everything staged in the current unit of work (aggregate changes and, when the
/// adapter wires the transactional outbox, any event staged via <see cref="IEventPublisher"/>)
/// as a single atomic operation.
/// </summary>
public interface IUnitOfWork
{
    Task SalvarAlteracoesAsync(CancellationToken cancellationToken);
}
