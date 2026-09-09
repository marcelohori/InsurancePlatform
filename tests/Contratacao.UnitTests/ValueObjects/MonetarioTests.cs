using Contratacao.Domain.Exceptions;
using Contratacao.Domain.ValueObjects;
using Xunit;

namespace Contratacao.UnitTests.ValueObjects;

public class MonetarioTests
{
    [Fact]
    public void Criar_Com_Quantia_Negativa_Lanca_DomainException()
    {
        Assert.Throws<DomainException>(() => Monetario.Criar(-1m));
    }

    [Fact]
    public void Criar_Com_Quantia_Valida_Retorna_Instancia()
    {
        var monetario = Monetario.Criar(1200m);

        Assert.Equal(1200m, monetario.Quantia);
        Assert.Equal("BRL", monetario.Moeda);
    }

    [Fact]
    public void Criar_Com_Mais_De_Duas_Casas_Decimais_Lanca_DomainException()
    {
        Assert.Throws<DomainException>(() => Monetario.Criar(1200.001m));
    }

    [Fact]
    public void Criar_Com_Codigo_De_Moeda_Invalido_Lanca_DomainException()
    {
        Assert.Throws<DomainException>(() => Monetario.Criar(1200m, "R1L"));
    }
}


