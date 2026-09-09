namespace Proposta.Domain;

/// <summary>
/// Explicit state machine for Proposta status transitions. Centralizes and documents
/// all valid transitions and prevents invalid state changes at the domain level.
/// </summary>
public static class StatusPropostaTransitions
{
    /// <summary>
    /// Returns all valid next states for a given current status.
    /// </summary>
    public static IReadOnlySet<StatusProposta> ObterTransicoesValidas(StatusProposta statusAtual) =>
        statusAtual switch
        {
            StatusProposta.EmAnalise => new HashSet<StatusProposta> { StatusProposta.Aprovada, StatusProposta.Rejeitada },
            StatusProposta.Aprovada => new HashSet<StatusProposta>(),
            StatusProposta.Rejeitada => new HashSet<StatusProposta>(),
            _ => throw new InvalidOperationException($"Status desconhecido: {statusAtual}")
        };

    /// <summary>
    /// Verifies if a transition from one status to another is valid.
    /// </summary>
    public static bool PodeTransicionar(StatusProposta de, StatusProposta para) =>
        ObterTransicoesValidas(de).Contains(para);

    /// <summary>
    /// Gets a human-readable description of valid transitions for a status.
    /// </summary>
    public static string DescricaoTransicoesValidas(StatusProposta statusAtual)
    {
        var validas = ObterTransicoesValidas(statusAtual);
        return validas.Count == 0
            ? "Nenhuma transição permitida (status final)"
            : string.Join(", ", validas.Select(s => $"'{s}'"));
    }
}
