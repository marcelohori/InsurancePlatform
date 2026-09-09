using MassTransit;
using Proposta.Application.Ports;

namespace Proposta.Infrastructure.Messaging;

/// <summary>
/// Stages the event on the bus outbox associated with the current DbContext. The message is
/// only handed to RabbitMQ after <see cref="IUnitOfWork.SalvarAlteracoesAsync"/> commits.
/// </summary>
public sealed class MassTransitEventPublisher(IPublishEndpoint publishEndpoint) : IEventPublisher
{
    public Task PublicarAsync<TEvento>(TEvento evento, CancellationToken cancellationToken)
        where TEvento : class =>
        publishEndpoint.Publish(evento, cancellationToken);
}
