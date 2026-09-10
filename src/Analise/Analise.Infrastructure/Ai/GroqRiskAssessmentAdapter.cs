using System.Net.Http.Json;
using System.Text.Json;
using Analise.Application.Exceptions;
using Analise.Application.Ports;
using Analise.Domain;
using Microsoft.Extensions.Configuration;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace Analise.Infrastructure.Ai;

/// <summary>
/// Calls the Groq API (OpenAI-compatible chat completions) to produce an advisory risk
/// score/recommendation for a proposal - a free-tier alternative to Anthropic for local testing,
/// selected via the "Analise:Provider" configuration. Same resilience/response-contract pattern
/// as <see cref="AnthropicRiskAssessmentAdapter"/>; only the request/response envelope differs.
/// </summary>
public sealed class GroqRiskAssessmentAdapter : IRiskAssessmentPort
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _model;

    public GroqRiskAssessmentAdapter(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiKey = configuration["Groq:ApiKey"]
            ?? throw new InvalidOperationException("Configuração 'Groq:ApiKey' não definida.");
        _model = configuration["Groq:Model"] ?? "qwen/qwen3.8-27b";
    }

    public async Task<RiskAssessmentResult> AvaliarAsync(RiskAssessmentInput input, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/openai/v1/chat/completions")
        {
            Content = JsonContent.Create(
                new
                {
                    model = _model,
                    max_tokens = 300,
                    temperature = 0,
                    response_format = new { type = "json_object" },
                    messages = new[] { new { role = "user", content = RiskAssessmentJsonContract.MontarPrompt(input) } },
                },
                options: JsonOptions),
        };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or BrokenCircuitException or TimeoutRejectedException)
        {
            throw new RiskAssessmentIndisponivelException("Provedor de IA está indisponível no momento.", ex);
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new RiskAssessmentIndisponivelException(
                "Provedor de IA retornou uma resposta inválida.",
                new HttpRequestException($"Resposta HTTP inesperada: {(int)response.StatusCode} ({response.ReasonPhrase})."));
        }

        GroqResponse? payload;
        try
        {
            payload = await response.Content.ReadFromJsonAsync<GroqResponse>(JsonOptions, cancellationToken);
        }
        catch (JsonException ex)
        {
            throw new RiskAssessmentIndisponivelException("Resposta inválida do provedor de IA.", ex);
        }
        var texto = payload?.Choices?.FirstOrDefault()?.Message?.Content;

        if (string.IsNullOrWhiteSpace(texto))
        {
            throw new RiskAssessmentIndisponivelException(
                "Resposta vazia do provedor de IA.",
                new InvalidOperationException("Empty content in Groq response."));
        }

        return RiskAssessmentJsonContract.InterpretarResposta(texto);
    }

    private sealed record GroqResponse(List<GroqChoice>? Choices);

    private sealed record GroqChoice(GroqMessage? Message);

    private sealed record GroqMessage(string? Content);
}
