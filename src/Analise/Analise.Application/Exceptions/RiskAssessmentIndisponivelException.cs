namespace Analise.Application.Exceptions;

/// <summary>
/// Raised by an <see cref="Ports.IRiskAssessmentPort"/> adapter when the AI provider is
/// unreachable or times out after the configured retries/circuit breaker are exhausted.
/// </summary>
public sealed class RiskAssessmentIndisponivelException : Exception
{
    public RiskAssessmentIndisponivelException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
