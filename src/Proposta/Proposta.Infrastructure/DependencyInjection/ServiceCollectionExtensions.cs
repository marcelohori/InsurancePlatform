using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Proposta.Application.Ports;
using Proposta.Infrastructure.Messaging;
using Proposta.Infrastructure.Persistence;

namespace Proposta.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPropostaInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("PropostaDb")
            ?? throw new InvalidOperationException("Connection string 'PropostaDb' não configurada.");

        services.AddDbContext<PropostaDbContext>(options =>
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorCodesToAdd: null);
                npgsqlOptions.CommandTimeout(30);
            }));

        services.AddScoped<IPropostaRepository, EfPropostaRepository>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddScoped<IEventPublisher, MassTransitEventPublisher>();

        services.AddMassTransit(busConfigurator =>
        {
            busConfigurator.AddEntityFrameworkOutbox<PropostaDbContext>(outbox =>
            {
                outbox.UsePostgres();
                outbox.UseBusOutbox();
            });

            busConfigurator.UsingRabbitMq((context, rabbitMqConfigurator) =>
            {
                var host = configuration["RabbitMq:Host"] ?? "localhost";
                var port = configuration.GetValue<ushort?>("RabbitMq:Port") ?? 5672;

                rabbitMqConfigurator.Host(
                    new Uri($"rabbitmq://{host}:{port}/"),
                    hostConfigurator =>
                    {
                        hostConfigurator.Username(configuration["RabbitMq:Username"]
                            ?? throw new InvalidOperationException("Configuração 'RabbitMq:Username' não definida."));
                        hostConfigurator.Password(configuration["RabbitMq:Password"]
                            ?? throw new InvalidOperationException("Configuração 'RabbitMq:Password' não definida."));
                    });

                rabbitMqConfigurator.ConfigureEndpoints(context);
            });
        });

        return services;
    }
}
