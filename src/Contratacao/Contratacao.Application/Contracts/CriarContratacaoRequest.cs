namespace Contratacao.Application.Contracts;

public sealed record CriarContratacaoRequest(
    Guid PropostaId,
    DateOnly DataContratacao,
    DateOnly? DataInicioVigencia,
    DateOnly? DataFimVigencia,
    decimal ValorPremio,
    string? MoedaValorPremio);
