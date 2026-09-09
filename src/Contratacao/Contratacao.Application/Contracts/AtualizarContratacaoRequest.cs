using Contratacao.Domain;

namespace Contratacao.Application.Contracts;

public sealed record AtualizarContratacaoRequest(
    DateOnly DataInicioVigencia,
    DateOnly DataFimVigencia,
    decimal ValorPremio,
    string? MoedaValorPremio,
    StatusContratacao Status);
