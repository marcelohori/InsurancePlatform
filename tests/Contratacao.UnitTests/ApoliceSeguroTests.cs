using Contratacao.Domain;
using Contratacao.Domain.Exceptions;
using Contratacao.Domain.ValueObjects;
using Xunit;

namespace Contratacao.UnitTests;

public class ApoliceSeguroTests
{
    private static ApoliceSeguro CriarApoliceValida() => Domain.ApoliceSeguro.Criar(
        Guid.NewGuid(),
        new DateOnly(2026, 1, 1),
        Monetario.Criar(1200m));

    [Fact]
    public void Criar_Retorna_Apolice_Ativa_Com_Numero_Gerado_E_Vigencia_Padrao()
    {
        var apolice = CriarApoliceValida();

        Assert.Equal(StatusContratacao.Ativa, apolice.Status);
        Assert.StartsWith("AP-20260101-", apolice.NumeroApolice, StringComparison.Ordinal);
        Assert.Equal(new DateOnly(2027, 1, 1), apolice.Vigencia.DataFim);
    }

    [Fact]
    public void Criar_Com_Vigencia_Explicita_Usa_A_Vigencia_Informada()
    {
        var vigencia = Vigencia.Criar(new DateOnly(2026, 1, 1), new DateOnly(2026, 3, 1));

        var apolice = Domain.ApoliceSeguro.Criar(Guid.NewGuid(), new DateOnly(2026, 1, 1), Monetario.Criar(1200m), vigencia);

        Assert.Equal(vigencia, apolice.Vigencia);
    }

    [Fact]
    public void Cancelar_Apolice_Ativa_Transiciona_Para_Cancelada()
    {
        var apolice = CriarApoliceValida();

        apolice.Cancelar();

        Assert.Equal(StatusContratacao.Cancelada, apolice.Status);
    }

    [Fact]
    public void Cancelar_Apolice_Ja_Cancelada_Lanca_ConflictDomainException()
    {
        var apolice = CriarApoliceValida();
        apolice.Cancelar();

        Assert.Throws<ConflictDomainException>(apolice.Cancelar);
    }
}


