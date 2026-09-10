using System.Text.Json;
using Analise.Application.Exceptions;
using Analise.Application.Ports;
using Analise.Domain;

namespace Analise.Infrastructure.Ai;

/// <summary>
/// Prompt and response-parsing logic shared by every LLM provider adapter. Every provider is
/// asked for the exact same JSON contract, so this stays provider-agnostic - only how the
/// prompt gets sent and the raw text gets extracted from the provider's response envelope
/// differs per adapter.
/// </summary>
internal static class RiskAssessmentJsonContract
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static string MontarPrompt(RiskAssessmentInput input) => $$"""
        Você é um assistente de subscrição de seguros. Avalie o risco da proposta abaixo e
        responda APENAS com um JSON válido, sem nenhum texto adicional, no formato exato:
        {"score": <inteiro de 0 a 100>, "recomendacao": "Aprovar" ou "Rejeitar", "justificativa": "<até 2 frases>"}

        Dados da proposta:
        - Tipo de seguro: {{input.TipoSeguro}}
        - Valor de cobertura: {{input.ValorCobertura:0.00}}
        - Valor de prêmio: {{input.ValorPremio:0.00}}
        """;

    public static RiskAssessmentResult InterpretarResposta(string texto)
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

    private static string ExtrairJson(string texto)
    {
        var inicio = texto.IndexOf('{');
        var fim = texto.LastIndexOf('}');
        return inicio >= 0 && fim > inicio ? texto[inicio..(fim + 1)] : texto;
    }

    private sealed record AvaliacaoRiscoResponse(int Score, string Recomendacao, string Justificativa);
}
