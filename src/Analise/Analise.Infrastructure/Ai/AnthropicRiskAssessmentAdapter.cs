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
/// Calls the Anthropic Messages API to produce an advisory risk score/recommendation for a
/// proposal. Resilience (retry, timeout, circuit breaker) is configured on the named HttpClient
/// via Microsoft.Extensions.Http.Resilience, not here - this adapter only translates the outcome.
/// </summary>
public sealed class AnthropicRiskAssessmentAdapter : IRiskAssessmentPort
{
    private const string AnthropicVersion = "2023-06-01";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _model;

    public AnthropicRiskAssessmentAdapter(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiKey = configuration["Anthropic:ApiKey"]
            ?? throw new InvalidOperationException("Configuração 'Anthropic:ApiKey' não definida.");
        _model = configuration["Anthropic:Model"] ?? "claude-haiku-4-5-20251001";
    }

    public async Task<RiskAssessmentResult> AvaliarAsync(RiskAssessmentInput input, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/v1/messages")
        {
            Content = JsonContent.Create(
                new
                {
                    model = _model,
                    max_tokens = 300,
                    messages = new[] { new { role = "user", content = MontarPrompt(input) } },
                },
                options: JsonOptions),
        };
        request.Headers.Add("x-api-key", _apiKey);
        request.Headers.Add("anthropic-version", AnthropicVersion);

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

        AnthropicResponse? payload;
        try
        {
            payload = await response.Content.ReadFromJsonAsync<AnthropicResponse>(JsonOptions, cancellationToken);
        }
        catch (JsonException ex)
        {
            throw new RiskAssessmentIndisponivelException("Resposta inválida do provedor de IA.", ex);
        }
        var texto = payload?.Content?.FirstOrDefault(c => c.Type == "text")?.Text;

        if (string.IsNullOrWhiteSpace(texto))
        {
            throw new RiskAssessmentIndisponivelException(
                "Resposta vazia do provedor de IA.",
                new InvalidOperationException("Empty content in Anthropic response."));
        }

        return InterpretarResposta(texto);
    }

    private static RiskAssessmentResult InterpretarResposta(string texto)
    {
        try
        {
            var avaliacao = JsonSerializer.Deserialize<AvaliacaoRiscoResponse>(ExtrairJson(texto), JsonOptions)
                ?? throw new InvalidOperationException("Resposta do provedor de IA não pôde ser interpretada.");

            if (avaliacao.Score is < 0 or > 100)
            {
                throw new InvalidOperationException("Score de risco fora do intervalo permitido.");
            }

            if (!Enum.TryParse<Recomendacao>(avaliacao.Recomendacao, ignoreCase: true, out var recomendacao)
                || !Enum.IsDefined(recomendacao))
            {
                throw new InvalidOperationException("Recomendação de risco inválida.");
            }

            if (string.IsNullOrWhiteSpace(avaliacao.Justificativa)
                || avaliacao.Justificativa.Length > 500)
            {
                throw new InvalidOperationException("Justificativa de risco ausente ou acima do limite.");
            }

            return new RiskAssessmentResult(avaliacao.Score, recomendacao, avaliacao.Justificativa.Trim());
        }
        catch (Exception ex) when (ex is JsonException or FormatException or InvalidOperationException)
        {
            throw new RiskAssessmentIndisponivelException("Resposta do provedor de IA em formato inesperado.", ex);
        }
    }

    private static string MontarPrompt(RiskAssessmentInput input) => $$"""
        Você é um assistente de subscrição de seguros. Avalie o risco da proposta abaixo e
        responda APENAS com um JSON válido, sem nenhum texto adicional, no formato exato:
        {"score": <inteiro de 0 a 100>, "recomendacao": "Aprovar" ou "Rejeitar", "justificativa": "<até 2 frases>"}

        Dados da proposta:
        - Tipo de seguro: {{input.TipoSeguro}}
        - Valor de cobertura: {{input.ValorCobertura:0.00}}
        - Valor de prêmio: {{input.ValorPremio:0.00}}
        """;

    private static string ExtrairJson(string texto)
    {
        var inicio = texto.IndexOf('{');
        var fim = texto.LastIndexOf('}');
        return inicio >= 0 && fim > inicio ? texto[inicio..(fim + 1)] : texto;
    }

    private sealed record AnthropicResponse(List<AnthropicContentBlock>? Content);

    private sealed record AnthropicContentBlock(string Type, string? Text);

    private sealed record AvaliacaoRiscoResponse(int Score, string Recomendacao, string Justificativa);
}
