namespace Proposta.Application.Exceptions;

/// <summary>Raised when a requested Proposta id does not exist. Adapters map this to HTTP 404.</summary>
public sealed class PropostaNaoEncontradaException : Exception
{
    public PropostaNaoEncontradaException(Guid id)
        : base($"Proposta '{id}' não encontrada.")
    {
    }
}
