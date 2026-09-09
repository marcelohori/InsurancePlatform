using Analise.Application.Ports;
using Analise.Infrastructure.Ai;
using Analise.Infrastructure.Messaging;
using Analise.Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Analise.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAnaliseInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("AnaliseDb")
            ?? throw new InvalidOperationException("Connection string 'AnaliseDb' não configurada.");

        services.AddDbContext<AnaliseDbContext>(options =>
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorCodesToAdd: null);
                npgsqlOptions.CommandTimeout(30);
            }));

        services.AddScoped<IAnaliseRepository, EfAnaliseRepository>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();

        var anthropicBaseUrl = configuration["Anthropic:BaseUrl"]
            ?? throw new InvalidOperationException("Configuração 'Anthropic:BaseUrl' não definida.");
        var allowInsecureAnthropic = configuration.GetValue<bool>("Anthropic:AllowInsecureHttpForDevelopment");
        if (!Uri.TryCreate(anthropicBaseUrl, UriKind.Absolute, out var anthropicUri)
            || (anthropicUri.Scheme != Uri.UriSchemeHttps
                && !(allowInsecureAnthropic && anthropicUri.Scheme == Uri.UriSchemeHttp)))
        {
            throw new InvalidOperationException("Configuração 'Anthropic:BaseUrl' deve ser uma URL HTTPS válida.");
        }

        var resilienceTimeoutSeconds = configuration.GetValue<int?>("Anthropic:TimeoutSeconds") ?? 20;
        var resilienceRetryAttempts = configuration.GetValue<int?>("Anthropic:RetryAttempts") ?? 1;
        if (resilienceTimeoutSeconds is < 1 or > 120 || resilienceRetryAttempts is < 0 or > 3)
        {
            throw new InvalidOperationException("A configuração de resiliência do provedor de IA está fora dos limites permitidos.");
        }

        var aiTotalTimeoutSeconds = resilienceTimeoutSeconds * resilienceRetryAttempts + resilienceTimeoutSeconds;

        services
            .AddHttpClient<IRiskAssessmentPort, AnthropicRiskAssessmentAdapter>(client =>
            {
                client.BaseAddress = anthropicUri;
                client.Timeout = TimeSpan.FromSeconds(Math.Min(aiTotalTimeoutSeconds, 90));
            })
            .AddStandardResilienceHandler(options =>
            {
                options.Retry.MaxRetryAttempts = resilienceRetryAttempts;
                options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(resilienceTimeoutSeconds);
                options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(Math.Min(aiTotalTimeoutSeconds, 90));
                // Circuit-breaker: open after repeated failures, half-open after 60s to retry (AI calls can take longer)
                options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(60);
                options.CircuitBreaker.FailureRatio = 0.5;
            });

        services.AddMassTransit(busConfigurator =>
        {
            // UsePostgres() registers the EF outbox/inbox schema. UseBusOutbox() (the send-side
            // producer outbox) is deliberately omitted - Analise.Api only consumes, it never
            // publishes, so there is nothing for a producer outbox to intercept here.
            busConfigurator.AddEntityFrameworkOutbox<AnaliseDbContext>(outbox => outbox.UsePostgres());

            busConfigurator.AddConsumer<PropostaCriadaEventConsumer>();

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

                // Explicit endpoint (instead of the ConfigureEndpoints convention) so the EF inbox
                // middleware - which dedups by MessageId - can be attached to it directly.
                rabbitMqConfigurator.ReceiveEndpoint("proposta-criada-event", endpointConfigurator =>
                {
                    endpointConfigurator.PrefetchCount = 16;
                    endpointConfigurator.UseMessageRetry(retry => retry.Exponential(
                        retryLimit: 3,
                        minInterval: TimeSpan.FromSeconds(1),
                        maxInterval: TimeSpan.FromSeconds(30),
                        intervalDelta: TimeSpan.FromSeconds(2)));
                    endpointConfigurator.UseKillSwitch(options => options
                        .SetActivationThreshold(10)
                        .SetTripThreshold(0.15)
                        .SetRestartTimeout(m: 1));
                    endpointConfigurator.UseEntityFrameworkOutbox<AnaliseDbContext>(context);
                    endpointConfigurator.ConfigureConsumer<PropostaCriadaEventConsumer>(context);
                });
            });
        });

        return services;
    }
}
