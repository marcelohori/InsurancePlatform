using Proposta.Domain.Exceptions;
using Proposta.Domain.ValueObjects;
using Xunit;

namespace Proposta.UnitTests.ValueObjects;

public class MonetarioTests
{
    [Fact]
    public void Criar_Com_Mais_De_Duas_Casas_Decimais_Lanca_DomainException()
    {
        Assert.Throws<DomainException>(() => Monetario.Criar(10.001m));
    }

    [Theory]
    [InlineData("R1L")]
    [InlineData("BR-")]
    public void Criar_Com_Codigo_De_Moeda_Invalido_Lanca_DomainException(string moeda)
    {
        Assert.Throws<DomainException>(() => Monetario.Criar(10m, moeda));
    }
}
