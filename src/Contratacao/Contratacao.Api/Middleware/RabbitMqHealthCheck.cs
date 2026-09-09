using Microsoft.Extensions.Diagnostics.HealthChecks;
using RabbitMQ.Client;

namespace Contratacao.Api.Middleware;

public sealed class RabbitMqHealthCheck(IConfiguration configuration) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var factory = new ConnectionFactory
            {
                HostName = configuration["RabbitMq:Host"] ?? throw new InvalidOperationException("Configuração 'RabbitMq:Host' não definida."),
                Port = configuration.GetValue<ushort?>("RabbitMq:Port") ?? 5672,
                UserName = configuration["RabbitMq:Username"] ?? throw new InvalidOperationException("Configuração 'RabbitMq:Username' não definida."),
                Password = configuration["RabbitMq:Password"] ?? throw new InvalidOperationException("Configuração 'RabbitMq:Password' não definida."),
            };
            await using var connection = await factory.CreateConnectionAsync(cancellationToken);
            return connection.IsOpen ? HealthCheckResult.Healthy() : HealthCheckResult.Unhealthy("Conexão RabbitMQ não está aberta.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("RabbitMQ indisponível.", exception);
        }
    }
}
