using Analise.Domain;

namespace Analise.Application.Ports;

/// <summary>
/// Outbound port for the AI-assisted risk assessment call. Implemented by an adapter that
/// talks to the LLM provider - Application never knows which provider or SDK is used.
/// </summary>
public interface IRiskAssessmentPort
{
    Task<RiskAssessmentResult> AvaliarAsync(RiskAssessmentInput input, CancellationToken cancellationToken);
}

public sealed record RiskAssessmentInput(Guid PropostaId, string TipoSeguro, decimal ValorCobertura, decimal ValorPremio);

public sealed record RiskAssessmentResult(int ScoreRisco, Recomendacao Recomendacao, string Justificativa);
