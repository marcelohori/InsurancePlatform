using Proposta.Domain.ValueObjects;

namespace Proposta.Application.Dtos;

public sealed record CriarPropostaCommand(
    string NomeSegurado,
    string DocumentoSegurado,
    string TipoSeguro,
    decimal ValorCobertura,
    string? MoedaCobertura,
    decimal ValorPremio,
    string? MoedaPremio,
    string CriadoPor = "")
{
    public string MoedaCoberturaOuPadrao => MoedaCobertura ?? Monetario.MoedaPadrao;

    public string MoedaPremioOuPadrao => MoedaPremio ?? Monetario.MoedaPadrao;
}
