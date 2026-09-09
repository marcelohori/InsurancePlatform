using Contratacao.Application.Ports;
using MassTransit;

namespace Contratacao.Infrastructure.Messaging;

public sealed class MassTransitEventPublisher(IPublishEndpoint publishEndpoint) : IEventPublisher
{
    public Task PublicarAsync<TEvento>(TEvento evento, CancellationToken cancellationToken)
        where TEvento : class =>
        publishEndpoint.Publish(evento, cancellationToken);
}
