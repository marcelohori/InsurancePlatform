using Contratacao.Application.Ports;
using Contratacao.Infrastructure.Http;
using Contratacao.Infrastructure.Messaging;
using Contratacao.Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Contratacao.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddContratacaoInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("ContratacaoDb")
            ?? throw new InvalidOperationException("Connection string 'ContratacaoDb' não configurada.");

        services.AddDbContext<ContratacaoDbContext>(options =>
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorCodesToAdd: null);
                npgsqlOptions.CommandTimeout(30);
            }));

        services.AddScoped<IContratacaoRepository, EfContratacaoRepository>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddScoped<IIdempotencyStore, EfIdempotencyStore>();
        services.AddScoped<IEventPublisher, MassTransitEventPublisher>();

        services.AddHttpContextAccessor();

        var propostaApiBaseUrl = configuration["PropostaApi:BaseUrl"]
            ?? throw new InvalidOperationException("Configuração 'PropostaApi:BaseUrl' não definida.");

        if (!Uri.TryCreate(propostaApiBaseUrl, UriKind.Absolute, out var propostaApiUri)
            || propostaApiUri.Scheme is not ("http" or "https"))
        {
            throw new InvalidOperationException("Configuração 'PropostaApi:BaseUrl' deve ser uma URL HTTP/HTTPS válida.");
        }

        var timeoutSeconds = configuration.GetValue<int?>("PropostaApi:TimeoutSeconds") ?? 5;
        var retryAttempts = configuration.GetValue<int?>("PropostaApi:RetryAttempts") ?? 2;
        if (timeoutSeconds is < 1 or > 30 || retryAttempts is < 0 or > 5)
        {
            throw new InvalidOperationException("A configuração de resiliência da PropostaApi está fora dos limites permitidos.");
        }

        var totalTimeoutSeconds = timeoutSeconds * retryAttempts + timeoutSeconds;

        services
            .AddHttpClient<IPropostaVerificationPort, HttpPropostaVerificationAdapter>(client =>
            {
                client.BaseAddress = propostaApiUri;
                client.Timeout = TimeSpan.FromSeconds(Math.Min(totalTimeoutSeconds, 30));
            })
            .AddStandardResilienceHandler(options =>
            {
                options.Retry.MaxRetryAttempts = retryAttempts;
                options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
                options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(Math.Min(totalTimeoutSeconds, 30));
                // Circuit-breaker: open after 5 consecutive failures, half-open after 30s to retry
                options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
                options.CircuitBreaker.FailureRatio = 0.5;
            });

        services.AddMassTransit(busConfigurator =>
        {
            busConfigurator.AddEntityFrameworkOutbox<ContratacaoDbContext>(outbox =>
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
