using Proposta.Domain;

namespace Proposta.Application.Contracts;

public sealed record AtualizarPropostaRequest(
    string NomeSegurado,
    string DocumentoSegurado,
    string TipoSeguro,
    decimal ValorCobertura,
    string? MoedaCobertura,
    decimal ValorPremio,
    string? MoedaPremio,
    StatusProposta Status);
