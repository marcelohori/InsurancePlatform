using Analise.Domain.Exceptions;

namespace Analise.Domain;

/// <summary>Aggregate root for the Analise bounded context - an AI-assisted risk assessment of a proposal.</summary>
public sealed class AnaliseRisco
{
    public Guid Id { get; }

    public Guid PropostaId { get; }

    public StatusAnalise Status { get; private set; }

    public int? ScoreRisco { get; private set; }

    public Recomendacao? Recomendacao { get; private set; }

    public string? Justificativa { get; private set; }

    public DateTimeOffset DataCriacao { get; }

    public DateTimeOffset? DataConclusao { get; private set; }

#pragma warning disable CS8618 // Materialized exclusively by EF Core via reflection; all properties are set from the stored row.
    private AnaliseRisco()
    {
    }
#pragma warning restore CS8618

    private AnaliseRisco(
        Guid id,
        Guid propostaId,
        StatusAnalise status,
        int? scoreRisco,
        Recomendacao? recomendacao,
        string? justificativa,
        DateTimeOffset dataCriacao,
        DateTimeOffset? dataConclusao)
    {
        Id = id;
        PropostaId = propostaId;
        Status = status;
        ScoreRisco = scoreRisco;
        Recomendacao = recomendacao;
        Justificativa = justificativa;
        DataCriacao = dataCriacao;
        DataConclusao = dataConclusao;
    }

    public static AnaliseRisco Iniciar(Guid propostaId, TimeProvider? timeProvider = null)
    {
        var agora = (timeProvider ?? TimeProvider.System).GetUtcNow();
        return new AnaliseRisco(Guid.NewGuid(), propostaId, StatusAnalise.EmProcessamento, null, null, null, agora, null);
    }

    public static AnaliseRisco Reidratar(
        Guid id,
        Guid propostaId,
        StatusAnalise status,
        int? scoreRisco,
        Recomendacao? recomendacao,
        string? justificativa,
        DateTimeOffset dataCriacao,
        DateTimeOffset? dataConclusao) =>
        new(id, propostaId, status, scoreRisco, recomendacao, justificativa, dataCriacao, dataConclusao);

    public void ConcluirComSucesso(int scoreRisco, Recomendacao recomendacao, string justificativa, TimeProvider? timeProvider = null)
    {
        GarantirEmProcessamento();

        if (scoreRisco is < 0 or > 100)
        {
            throw new DomainException("O score de risco deve estar entre 0 e 100.");
        }

        ScoreRisco = scoreRisco;
        Recomendacao = recomendacao;
        Justificativa = justificativa;
        Status = StatusAnalise.Concluida;
        DataConclusao = (timeProvider ?? TimeProvider.System).GetUtcNow();
    }

    public void ConcluirComFalha(string motivo, TimeProvider? timeProvider = null)
    {
        GarantirEmProcessamento();

        Justificativa = motivo;
        Status = StatusAnalise.Falha;
        DataConclusao = (timeProvider ?? TimeProvider.System).GetUtcNow();
    }

    /// <summary>Allows a failed analysis to be retried.</summary>
    public void Reiniciar()
    {
        if (Status != StatusAnalise.Falha)
        {
            throw new ConflictDomainException($"A análise {Id} só pode ser reiniciada a partir do status 'Falha' (status atual: '{Status}').");
        }

        LimparResultado();
    }

    public void ReiniciarQuandoExpirada()
    {
        if (Status != StatusAnalise.EmProcessamento)
        {
            throw new ConflictDomainException(
                $"A análise {Id} só pode ser retomada a partir do status 'EmProcessamento' (status atual: '{Status}').");
        }

        LimparResultado();
    }

    private void LimparResultado()
    {
        Status = StatusAnalise.EmProcessamento;
        ScoreRisco = null;
        Recomendacao = null;
        Justificativa = null;
        DataConclusao = null;
    }

    private void GarantirEmProcessamento()
    {
        if (Status != StatusAnalise.EmProcessamento)
        {
            throw new ConflictDomainException($"A análise {Id} não está em processamento (status atual: '{Status}').");
        }
    }
}
