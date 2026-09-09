using Contratacao.Domain;

namespace Contratacao.Api.Contracts;

public sealed record AtualizarContratacaoRequest(
    DateOnly DataInicioVigencia,
    DateOnly DataFimVigencia,
    decimal ValorPremio,
    string? MoedaValorPremio,
    StatusContratacao Status);
