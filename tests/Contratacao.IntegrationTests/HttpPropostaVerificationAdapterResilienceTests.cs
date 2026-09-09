using Contratacao.Application.Exceptions;
using Contratacao.Application.Ports;
using Contratacao.Infrastructure.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Xunit;

namespace Contratacao.IntegrationTests;

/// <summary>
/// Verifies the resilience pipeline (retry + timeout + circuit breaker) around the call to
/// Proposta.Api: when the dependency is unreachable, the adapter fails fast with a typed
/// exception instead of hanging indefinitely.
/// </summary>
public class HttpPropostaVerificationAdapterResilienceTests
{
    [Fact]
    public async Task VerificarAsync_Com_Proposta_Api_Indisponivel_Lanca_PropostaServiceIndisponivelException()
    {
        var services = new ServiceCollection();
        services.AddHttpContextAccessor();

        services
            .AddHttpClient<IPropostaVerificationPort, HttpPropostaVerificationAdapter>(client =>
            {
                // Port 1 on loopback refuses connections immediately - simulates Proposta.Api being down
                // without waiting out a full network timeout.
                client.BaseAddress = new Uri("http://127.0.0.1:1");
            })
            .AddStandardResilienceHandler(options =>
            {
                options.Retry.MaxRetryAttempts = 2;
                options.Retry.Delay = TimeSpan.FromMilliseconds(50);
                options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(2);
                options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(10);
                options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(10);
            });

        await using var provider = services.BuildServiceProvider();
        var port = provider.GetRequiredService<IPropostaVerificationPort>();

        await Assert.ThrowsAsync<PropostaServiceIndisponivelException>(
            () => port.VerificarAsync(Guid.NewGuid(), CancellationToken.None));
    }
}


