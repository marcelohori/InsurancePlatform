using Contratacao.Domain;
using Contratacao.Domain.ValueObjects;

namespace Contratacao.Application.Dtos;

public sealed record AtualizarContratacaoCommand(
    Guid Id,
    DateOnly DataInicioVigencia,
    DateOnly DataFimVigencia,
    decimal ValorPremio,
    string? MoedaValorPremio,
    StatusContratacao Status)
{
    public string MoedaValorPremioOuPadrao => MoedaValorPremio ?? Monetario.MoedaPadrao;
}
