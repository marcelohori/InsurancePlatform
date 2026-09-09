using Proposta.Domain;

namespace Proposta.Application.Dtos;

public sealed record PropostaDto(
    Guid Id,
    string NomeSegurado,
    string DocumentoSegurado,
    TipoSeguro TipoSeguro,
    decimal ValorCobertura,
    string MoedaCobertura,
    decimal ValorPremio,
    string MoedaPremio,
    StatusProposta Status,
    DateTimeOffset DataCriacao)
{
    public static PropostaDto DeEntidade(PropostaSeguro proposta) => new(
        proposta.Id,
        proposta.NomeSegurado,
        proposta.DocumentoSegurado.Numero,
        proposta.TipoSeguro,
        proposta.ValorCobertura.Quantia,
        proposta.ValorCobertura.Moeda,
        proposta.ValorPremio.Quantia,
        proposta.ValorPremio.Moeda,
        proposta.Status,
        proposta.DataCriacao);
}
