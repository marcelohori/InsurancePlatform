using Proposta.Domain;
using Proposta.Domain.Exceptions;
using Proposta.Domain.ValueObjects;
using Xunit;

namespace Proposta.UnitTests;

public class PropostaSeguroTests
{
    private static PropostaSeguro CriarPropostaValida() =>
        Domain.PropostaSeguro.Criar(
            "Maria Silva",
            DocumentoIdentificacao.Criar("11144477735"),
            TipoSeguro.Auto,
            Monetario.Criar(50000m),
            Monetario.Criar(1200m));

    [Fact]
    public void Criar_Retorna_Proposta_Com_Status_EmAnalise()
    {
        var proposta = CriarPropostaValida();

        Assert.Equal(StatusProposta.EmAnalise, proposta.Status);
        Assert.NotEqual(Guid.Empty, proposta.Id);
    }

    [Fact]
    public void Criar_Com_Nome_Maior_Que_200_Caracteres_Lanca_DomainException()
    {
        Assert.Throws<DomainException>(() =>
            Domain.PropostaSeguro.Criar(
                new string('A', 201),
                DocumentoIdentificacao.Criar("11144477735"),
                TipoSeguro.Auto,
                Monetario.Criar(50000m),
                Monetario.Criar(1200m)));
    }

    [Fact]
    public void Criar_Sem_Nome_Segurado_Lanca_DomainException()
    {
        Assert.Throws<DomainException>(() =>
            Domain.PropostaSeguro.Criar(
                "   ",
                DocumentoIdentificacao.Criar("11144477735"),
                TipoSeguro.Auto,
                Monetario.Criar(50000m),
                Monetario.Criar(1200m)));
    }

    [Fact]
    public void Aprovar_Proposta_EmAnalise_Transiciona_Para_Aprovada()
    {
        var proposta = CriarPropostaValida();

        proposta.Aprovar();

        Assert.Equal(StatusProposta.Aprovada, proposta.Status);
    }

    [Fact]
    public void Rejeitar_Proposta_EmAnalise_Transiciona_Para_Rejeitada()
    {
        var proposta = CriarPropostaValida();

        proposta.Rejeitar();

        Assert.Equal(StatusProposta.Rejeitada, proposta.Status);
    }

    [Fact]
    public void Aprovar_Proposta_Ja_Aprovada_Lanca_ConflictDomainException()
    {
        var proposta = CriarPropostaValida();
        proposta.Aprovar();

        Assert.Throws<ConflictDomainException>(proposta.Aprovar);
    }

    [Fact]
    public void Rejeitar_Proposta_Ja_Rejeitada_Lanca_ConflictDomainException()
    {
        var proposta = CriarPropostaValida();
        proposta.Rejeitar();

        Assert.Throws<ConflictDomainException>(proposta.Rejeitar);
    }

    [Fact]
    public void GarantirQuePodeSerExcluida_Quando_Aprovada_Lanca_ConflictDomainException()
    {
        var proposta = CriarPropostaValida();
        proposta.Aprovar();

        Assert.Throws<ConflictDomainException>(proposta.GarantirQuePodeSerExcluida);
    }

    [Fact]
    public void PodeSerExcluida_E_Falso_Quando_Aprovada()
    {
        var proposta = CriarPropostaValida();
        proposta.Aprovar();

        Assert.False(proposta.PodeSerExcluida);
    }

    [Theory]
    [InlineData(StatusProposta.EmAnalise, true)]
    [InlineData(StatusProposta.Rejeitada, true)]
    public void PodeSerExcluida_E_Verdadeiro_Para_Status_Nao_Aprovada(StatusProposta status, bool esperado)
    {
        var proposta = CriarPropostaValida();
        if (status == StatusProposta.Rejeitada)
        {
            proposta.Rejeitar();
        }

        Assert.Equal(esperado, proposta.PodeSerExcluida);
    }
}


