using Analise.Application.Exceptions;
using Analise.Application.Ports;
using Analise.Domain;
using Analise.Infrastructure.Ai;
using Microsoft.Extensions.Configuration;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;

namespace Analise.IntegrationTests;

/// <summary>
/// Exercises GroqRiskAssessmentAdapter against a WireMock stand-in for Groq's (OpenAI-compatible)
/// chat completions API - no Postgres/RabbitMQ needed, this only proves the HTTP request shape,
/// response parsing and error mapping are correct, mirroring how the Anthropic adapter is proven
/// via AnaliseProcessingFixture's WireMock stub.
/// </summary>
public sealed class GroqRiskAssessmentAdapterTests : IDisposable
{
    private readonly WireMockServer _groqStub = WireMockServer.Start();

    [Fact]
    public async Task AvaliarAsync_RespostaValida_RetornaResultadoInterpretado()
    {
        _groqStub
            .Given(Request.Create().WithPath("/openai/v1/chat/completions").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(
                    """{"choices":[{"message":{"content":"{\"score\": 42, \"recomendacao\": \"Aprovar\", \"justificativa\": \"Risco baixo.\"}"}}]}"""));

        var resultado = await CriarAdapter().AvaliarAsync(
            new RiskAssessmentInput(Guid.NewGuid(), "Auto", 50000m, 1200m),
            CancellationToken.None);

        Assert.Equal(42, resultado.ScoreRisco);
        Assert.Equal(Recomendacao.Aprovar, resultado.Recomendacao);
        Assert.Equal("Risco baixo.", resultado.Justificativa);
    }

    [Fact]
    public async Task AvaliarAsync_ProvedorRetornaErro_LancaRiskAssessmentIndisponivelException()
    {
        _groqStub
            .Given(Request.Create().WithPath("/openai/v1/chat/completions").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(500));

        await Assert.ThrowsAsync<RiskAssessmentIndisponivelException>(() =>
            CriarAdapter().AvaliarAsync(new RiskAssessmentInput(Guid.NewGuid(), "Auto", 50000m, 1200m), CancellationToken.None));
    }

    [Fact]
    public async Task AvaliarAsync_ScoreForaDoIntervalo_LancaRiskAssessmentIndisponivelException()
    {
        _groqStub
            .Given(Request.Create().WithPath("/openai/v1/chat/completions").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(
                    """{"choices":[{"message":{"content":"{\"score\": 150, \"recomendacao\": \"Aprovar\", \"justificativa\": \"Inválido.\"}"}}]}"""));

        await Assert.ThrowsAsync<RiskAssessmentIndisponivelException>(() =>
            CriarAdapter().AvaliarAsync(new RiskAssessmentInput(Guid.NewGuid(), "Auto", 50000m, 1200m), CancellationToken.None));
    }

    private GroqRiskAssessmentAdapter CriarAdapter()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Groq:ApiKey"] = "test-key",
                ["Groq:Model"] = "qwen/qwen3.8-27b",
            })
            .Build();

        var httpClient = new HttpClient { BaseAddress = new Uri(_groqStub.Url!) };
        return new GroqRiskAssessmentAdapter(httpClient, configuration);
    }

    public void Dispose()
    {
        _groqStub.Stop();
        _groqStub.Dispose();
        GC.SuppressFinalize(this);
    }
}
