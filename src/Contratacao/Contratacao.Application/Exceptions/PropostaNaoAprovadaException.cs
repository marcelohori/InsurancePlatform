namespace Contratacao.Application.Exceptions;

/// <summary>Raised when the referenced Proposta exists but is not Aprovada. Adapters map this to HTTP 409.</summary>
public sealed class PropostaNaoAprovadaException : Exception
{
    public PropostaNaoAprovadaException(Guid propostaId, string statusAtual)
        : base($"Proposta '{propostaId}' não pode ser contratada: status atual é '{statusAtual}', esperado 'Aprovada'.")
    {
    }
}
