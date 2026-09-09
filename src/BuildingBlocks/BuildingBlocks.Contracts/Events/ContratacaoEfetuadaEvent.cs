namespace BuildingBlocks.Contracts.Events;

/// <summary>
/// Published by Contratacao.Api whenever a proposal is successfully converted into a policy.
/// </summary>
public sealed record ContratacaoEfetuadaEvent(
    Guid ContratacaoId,
    Guid PropostaId,
    string NumeroApolice,
    DateTimeOffset DataContratacao);
