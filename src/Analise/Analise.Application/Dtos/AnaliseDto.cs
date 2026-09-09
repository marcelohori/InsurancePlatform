using Analise.Domain;

namespace Analise.Application.Dtos;

public sealed record AnaliseDto(
    Guid Id,
    Guid PropostaId,
    StatusAnalise Status,
    int? ScoreRisco,
    Recomendacao? Recomendacao,
    string? Justificativa,
    DateTimeOffset DataCriacao,
    DateTimeOffset? DataConclusao)
{
    public static AnaliseDto DeEntidade(AnaliseRisco analise) => new(
        analise.Id,
        analise.PropostaId,
        analise.Status,
        analise.ScoreRisco,
        analise.Recomendacao,
        analise.Justificativa,
        analise.DataCriacao,
        analise.DataConclusao);
}
