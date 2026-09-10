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

        // "Analise:Provider" picks which IRiskAssessmentPort adapter is active - Claude
        // (Anthropic, the production default) or Groq (free tier, handy for local testing
        // without an Anthropic key). Only the selected provider's config is validated at
        // startup; the other one's section can be left empty.
        var provider = configuration["Analise:Provider"];
        if (string.Equals(provider, "Groq", StringComparison.OrdinalIgnoreCase))
        {
            AddAiHttpClient<GroqRiskAssessmentAdapter>(services, configuration, "Groq", "https://api.groq.com");
            services.AddScoped<IRiskAssessmentPort>(sp => sp.GetRequiredService<GroqRiskAssessmentAdapter>());
        }
        else
        {
            AddAiHttpClient<AnthropicRiskAssessmentAdapter>(services, configuration, "Anthropic", "https://api.anthropic.com");
            services.AddScoped<IRiskAssessmentPort>(sp => sp.GetRequiredService<AnthropicRiskAssessmentAdapter>());
        }

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

    /// <summary>
    /// Registers a typed HttpClient for an AI provider adapter (Anthropic, Groq, ...) with the
    /// same base-URL validation and retry/timeout/circuit-breaker resilience policy, reading
    /// config from "{sectionName}:BaseUrl" / "TimeoutSeconds" / "RetryAttempts" /
    /// "AllowInsecureHttpForDevelopment". <paramref name="defaultBaseUrl"/> is the provider's
    /// well-known public API endpoint - not a secret, safe to default so config only needs an
    /// override in unusual setups (e.g. an HTTP proxy in tests).
    /// </summary>
    private static void AddAiHttpClient<TClient>(IServiceCollection services, IConfiguration configuration, string sectionName, string defaultBaseUrl)
        where TClient : class
    {
        var baseUrl = configuration[$"{sectionName}:BaseUrl"] ?? defaultBaseUrl;
        var allowInsecure = configuration.GetValue<bool>($"{sectionName}:AllowInsecureHttpForDevelopment");
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri)
            || (baseUri.Scheme != Uri.UriSchemeHttps && !(allowInsecure && baseUri.Scheme == Uri.UriSchemeHttp)))
        {
            throw new InvalidOperationException($"Configuração '{sectionName}:BaseUrl' deve ser uma URL HTTPS válida.");
        }

        var timeoutSeconds = configuration.GetValue<int?>($"{sectionName}:TimeoutSeconds") ?? 20;
        var retryAttempts = configuration.GetValue<int?>($"{sectionName}:RetryAttempts") ?? 1;
        if (timeoutSeconds is < 1 or > 120 || retryAttempts is < 0 or > 3)
        {
            throw new InvalidOperationException($"A configuração de resiliência do provedor de IA ({sectionName}) está fora dos limites permitidos.");
        }

        var totalTimeoutSeconds = Math.Min(timeoutSeconds * retryAttempts + timeoutSeconds, 90);

        services
            .AddHttpClient<TClient>(client =>
            {
                client.BaseAddress = baseUri;
                client.Timeout = TimeSpan.FromSeconds(totalTimeoutSeconds);
            })
            .AddStandardResilienceHandler(options =>
            {
                options.Retry.MaxRetryAttempts = retryAttempts;
                options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
                options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(totalTimeoutSeconds);
                // Circuit-breaker: open after repeated failures, half-open after 60s to retry (AI calls can take longer)
                options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(60);
                options.CircuitBreaker.FailureRatio = 0.5;
            });
    }
}
