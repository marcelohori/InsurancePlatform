using Contratacao.Domain.Exceptions;
using Contratacao.Domain.ValueObjects;

namespace Contratacao.Domain;

/// <summary>
/// Aggregate root for the Contratacao bounded context. Named ApoliceSeguro (insurance policy)
/// to avoid colliding with the root namespace "Contratacao".
/// </summary>
public sealed class ApoliceSeguro
{
    public Guid Id { get; }

    public Guid PropostaId { get; }

    public string NumeroApolice { get; }

    public DateOnly DataContratacao { get; }

    public Vigencia Vigencia { get; private set; }

    public Monetario ValorPremio { get; private set; }

    public StatusContratacao Status { get; private set; }

#pragma warning disable CS8618 // Materialized exclusively by EF Core via reflection; all properties are set from the stored row.
    private ApoliceSeguro()
    {
    }
#pragma warning restore CS8618

    private ApoliceSeguro(
        Guid id,
        Guid propostaId,
        string numeroApolice,
        DateOnly dataContratacao,
        Vigencia vigencia,
        Monetario valorPremio,
        StatusContratacao status)
    {
        Id = id;
        PropostaId = propostaId;
        NumeroApolice = numeroApolice;
        DataContratacao = dataContratacao;
        Vigencia = vigencia;
        ValorPremio = valorPremio;
        Status = status;
    }

    public static ApoliceSeguro Criar(
        Guid propostaId,
        DateOnly dataContratacao,
        Monetario valorPremio,
        Vigencia? vigencia = null)
    {
        var vigenciaEfetiva = vigencia ?? Vigencia.CriarPadrao(dataContratacao);
        var numeroApolice = GerarNumeroApolice(dataContratacao);

        return new ApoliceSeguro(
            Guid.NewGuid(),
            propostaId,
            numeroApolice,
            dataContratacao,
            vigenciaEfetiva,
            valorPremio,
            StatusContratacao.Ativa);
    }

    public static ApoliceSeguro Reidratar(
        Guid id,
        Guid propostaId,
        string numeroApolice,
        DateOnly dataContratacao,
        Vigencia vigencia,
        Monetario valorPremio,
        StatusContratacao status) =>
        new(id, propostaId, numeroApolice, dataContratacao, vigencia, valorPremio, status);

    public void Cancelar()
    {
        if (Status != StatusContratacao.Ativa)
        {
            throw new ConflictDomainException($"A contratação {Id} já está '{Status}' e não pode ser cancelada novamente.");
        }

        Status = StatusContratacao.Cancelada;
    }

    public void AtualizarDados(Vigencia vigencia, Monetario valorPremio)
    {
        if (Status != StatusContratacao.Ativa)
        {
            throw new ConflictDomainException(
                $"A contratação {Id} não pode ser alterada no status '{Status}'.");
        }

        Vigencia = vigencia;
        ValorPremio = valorPremio;
    }

    private static string GerarNumeroApolice(DateOnly dataContratacao)
    {
        var sufixo = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        return $"AP-{dataContratacao:yyyyMMdd}-{sufixo}";
    }
}
