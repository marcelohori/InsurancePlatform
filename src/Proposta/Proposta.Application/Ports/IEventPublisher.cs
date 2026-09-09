namespace Proposta.Application.Ports;

/// <summary>
/// Outbound port for publishing domain events. The adapter is responsible for delivering
/// the event reliably (e.g. via a transactional outbox) - Application only knows it publishes.
/// </summary>
public interface IEventPublisher
{
    Task PublicarAsync<TEvento>(TEvento evento, CancellationToken cancellationToken)
        where TEvento : class;
}
