namespace Contratacao.Application.Exceptions;

/// <summary>
/// Raised when Proposta.Api is unreachable or times out after the configured retries/circuit
/// breaker are exhausted. Adapters map this to HTTP 503.
/// </summary>
public sealed class PropostaServiceIndisponivelException : Exception
{
    public PropostaServiceIndisponivelException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
