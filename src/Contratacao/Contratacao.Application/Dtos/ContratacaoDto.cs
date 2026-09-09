using Contratacao.Domain;

namespace Contratacao.Application.Dtos;

public sealed record ContratacaoDto(
    Guid Id,
    Guid PropostaId,
    string NumeroApolice,
    DateOnly DataContratacao,
    DateOnly DataInicioVigencia,
    DateOnly DataFimVigencia,
    decimal ValorPremio,
    string MoedaValorPremio,
    StatusContratacao Status)
{
    public static ContratacaoDto DeEntidade(ApoliceSeguro apolice) => new(
        apolice.Id,
        apolice.PropostaId,
        apolice.NumeroApolice,
        apolice.DataContratacao,
        apolice.Vigencia.DataInicio,
        apolice.Vigencia.DataFim,
        apolice.ValorPremio.Quantia,
        apolice.ValorPremio.Moeda,
        apolice.Status);
}
