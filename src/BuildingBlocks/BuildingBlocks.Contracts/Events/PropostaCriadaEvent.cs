namespace BuildingBlocks.Contracts.Events;

/// <summary>
/// Published by Proposta.Api whenever a new insurance proposal is created.
/// Consumed by Analise.Api to trigger the AI-assisted risk assessment.
/// </summary>
public sealed record PropostaCriadaEvent(
    Guid PropostaId,
    string NomeSegurado,
    string DocumentoSegurado,
    string TipoSeguro,
    decimal ValorCobertura,
    string MoedaValorCobertura,
    decimal ValorPremio,
    string MoedaValorPremio,
    DateTimeOffset DataCriacao);
