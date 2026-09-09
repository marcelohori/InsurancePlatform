using Contratacao.Domain.ValueObjects;

namespace Contratacao.Application.Dtos;

public sealed record CriarContratacaoCommand(
    Guid PropostaId,
    DateOnly DataContratacao,
    DateOnly? DataInicioVigencia,
    DateOnly? DataFimVigencia,
    decimal ValorPremio,
    string? MoedaValorPremio,
    string? IdempotencyKey)
{
    public string MoedaValorPremioOuPadrao => MoedaValorPremio ?? Monetario.MoedaPadrao;
}
