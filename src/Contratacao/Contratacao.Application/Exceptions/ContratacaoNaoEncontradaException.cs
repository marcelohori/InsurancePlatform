namespace Contratacao.Application.Exceptions;

/// <summary>Raised when a requested Contratacao id does not exist. Adapters map this to HTTP 404.</summary>
public sealed class ContratacaoNaoEncontradaException : Exception
{
    public ContratacaoNaoEncontradaException(Guid id)
        : base($"Contratação '{id}' não encontrada.")
    {
    }
}
