namespace Contratacao.Application.Ports;

public interface IEventPublisher
{
    Task PublicarAsync<TEvento>(TEvento evento, CancellationToken cancellationToken)
        where TEvento : class;
}
