namespace Analise.Application.Exceptions;

/// <summary>Raised when no risk assessment exists yet for the given proposal id. Adapters map this to HTTP 404.</summary>
public sealed class AnaliseNaoEncontradaException : Exception
{
    public AnaliseNaoEncontradaException(Guid propostaId)
        : base($"Nenhuma análise encontrada para a proposta '{propostaId}'.")
    {
    }
}
