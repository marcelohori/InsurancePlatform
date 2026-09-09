namespace Contratacao.Application.Exceptions;

/// <summary>Raised when the referenced Proposta does not exist. Adapters map this to HTTP 404.</summary>
public sealed class PropostaNaoEncontradaException : Exception
{
    public PropostaNaoEncontradaException(Guid propostaId)
        : base($"Proposta '{propostaId}' não encontrada.")
    {
    }
}
