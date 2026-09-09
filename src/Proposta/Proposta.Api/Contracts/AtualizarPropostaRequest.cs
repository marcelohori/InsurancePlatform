using Proposta.Domain;

namespace Proposta.Api.Contracts;

public sealed record AtualizarPropostaRequest(
    string NomeSegurado,
    string DocumentoSegurado,
    string TipoSeguro,
    decimal ValorCobertura,
    string? MoedaCobertura,
    decimal ValorPremio,
    string? MoedaPremio,
    StatusProposta Status);
