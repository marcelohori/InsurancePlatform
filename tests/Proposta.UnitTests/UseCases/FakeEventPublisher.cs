using Proposta.Application.Ports;

namespace Proposta.UnitTests.UseCases;

public sealed class FakeEventPublisher : IEventPublisher
{
    public List<object> EventosPublicados { get; } = [];

    public Task PublicarAsync<TEvento>(TEvento evento, CancellationToken cancellationToken)
        where TEvento : class
    {
        EventosPublicados.Add(evento);
        return Task.CompletedTask;
    }
}


