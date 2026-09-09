using Proposta.Domain;
using Proposta.Domain.ValueObjects;

namespace Proposta.Application.Dtos;

public sealed record AtualizarPropostaCommand(
    Guid Id,
    string NomeSegurado,
    string DocumentoSegurado,
    string TipoSeguro,
    decimal ValorCobertura,
    string? MoedaCobertura,
    decimal ValorPremio,
    string? MoedaPremio,
    StatusProposta Status)
{
    public string MoedaCoberturaOuPadrao => MoedaCobertura ?? Monetario.MoedaPadrao;

    public string MoedaPremioOuPadrao => MoedaPremio ?? Monetario.MoedaPadrao;
}
