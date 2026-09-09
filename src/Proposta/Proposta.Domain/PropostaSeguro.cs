using Proposta.Domain.Exceptions;
using Proposta.Domain.ValueObjects;

namespace Proposta.Domain;

/// <summary>
/// Aggregate root for the Proposta bounded context. Named PropostaSeguro to avoid
/// colliding with the root namespace "Proposta".
/// </summary>
public sealed class PropostaSeguro
{
    public Guid Id { get; }

    public string CriadoPor { get; private set; }

    public string NomeSegurado { get; private set; }

    public DocumentoIdentificacao DocumentoSegurado { get; private set; }

    public TipoSeguro TipoSeguro { get; private set; }

    public Monetario ValorCobertura { get; private set; }

    public Monetario ValorPremio { get; private set; }

    public StatusProposta Status { get; private set; }

    public DateTimeOffset DataCriacao { get; }

#pragma warning disable CS8618 // Materialized exclusively by EF Core via reflection; all properties are set from the stored row.
    private PropostaSeguro()
    {
    }
#pragma warning restore CS8618

    private PropostaSeguro(
        Guid id,
        string criadoPor,
        string nomeSegurado,
        DocumentoIdentificacao documentoSegurado,
        TipoSeguro tipoSeguro,
        Monetario valorCobertura,
        Monetario valorPremio,
        StatusProposta status,
        DateTimeOffset dataCriacao)
    {
        Id = id;
        CriadoPor = criadoPor;
        NomeSegurado = nomeSegurado;
        DocumentoSegurado = documentoSegurado;
        TipoSeguro = tipoSeguro;
        ValorCobertura = valorCobertura;
        ValorPremio = valorPremio;
        Status = status;
        DataCriacao = dataCriacao;
    }

    public static PropostaSeguro Criar(
        string nomeSegurado,
        DocumentoIdentificacao documentoSegurado,
        TipoSeguro tipoSeguro,
        Monetario valorCobertura,
        Monetario valorPremio,
        TimeProvider? timeProvider = null,
        string criadoPor = "legacy")
    {
        if (string.IsNullOrWhiteSpace(nomeSegurado) || nomeSegurado.Trim().Length > 200)
        {
            throw new DomainException("O nome do segurado é obrigatório e deve ter no máximo 200 caracteres.");
        }

        var agora = (timeProvider ?? TimeProvider.System).GetUtcNow();

        return new PropostaSeguro(
            Guid.NewGuid(),
            criadoPor,
            nomeSegurado.Trim(),
            documentoSegurado,
            tipoSeguro,
            valorCobertura,
            valorPremio,
            StatusProposta.EmAnalise,
            agora);
    }

    public static PropostaSeguro Reidratar(
        Guid id,
        string criadoPor,
        string nomeSegurado,
        DocumentoIdentificacao documentoSegurado,
        TipoSeguro tipoSeguro,
        Monetario valorCobertura,
        Monetario valorPremio,
        StatusProposta status,
        DateTimeOffset dataCriacao) =>
        new(id, criadoPor, nomeSegurado, documentoSegurado, tipoSeguro, valorCobertura, valorPremio, status, dataCriacao);

    public bool PodeSerExcluida => Status != StatusProposta.Aprovada;

    public bool PodeSerAtualizada => Status == StatusProposta.EmAnalise;

    public void Aprovar() => TransicionarPara(StatusProposta.Aprovada);

    public void Rejeitar() => TransicionarPara(StatusProposta.Rejeitada);

    public void GarantirQuePodeSerExcluida()
    {
        if (!PodeSerExcluida)
        {
            throw new ConflictDomainException($"A proposta {Id} está aprovada e não pode ser excluída.");
        }
    }

    public void GarantirQuePodeSerAtualizada()
    {
        if (!PodeSerAtualizada)
        {
            throw new ConflictDomainException($"A proposta {Id} está em status '{Status}' e não pode ser alterada. Apenas propostas em análise podem ser atualizadas.");
        }
    }

    private void TransicionarPara(StatusProposta novoStatus)
    {
        if (!StatusPropostaTransitions.PodeTransicionar(Status, novoStatus))
        {
            var transicoesValidas = StatusPropostaTransitions.DescricaoTransicoesValidas(Status);
            throw new ConflictDomainException(
                $"A proposta {Id} não pode transicionar de '{Status}' para '{novoStatus}'. Transições válidas: {transicoesValidas}.");
        }

        Status = novoStatus;
    }

    public void AtualizarDados(
        string nomeSegurado,
        DocumentoIdentificacao documentoSegurado,
        TipoSeguro tipoSeguro,
        Monetario valorCobertura,
        Monetario valorPremio)
    {
        if (string.IsNullOrWhiteSpace(nomeSegurado) || nomeSegurado.Trim().Length > 200)
        {
            throw new DomainException("O nome do segurado é obrigatório e deve ter no máximo 200 caracteres.");
        }

        NomeSegurado = nomeSegurado.Trim();
        DocumentoSegurado = documentoSegurado;
        TipoSeguro = tipoSeguro;
        ValorCobertura = valorCobertura;
        ValorPremio = valorPremio;
    }
}
