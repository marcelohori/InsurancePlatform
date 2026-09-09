using Contratacao.Domain.Exceptions;
using Contratacao.Domain.ValueObjects;
using Xunit;

namespace Contratacao.UnitTests.ValueObjects;

public class VigenciaTests
{
    [Fact]
    public void Criar_Com_Fim_Posterior_Ao_Inicio_E_Permitido()
    {
        var inicio = new DateOnly(2026, 1, 1);
        var fim = new DateOnly(2026, 6, 1);

        var vigencia = Vigencia.Criar(inicio, fim);

        Assert.Equal(inicio, vigencia.DataInicio);
        Assert.Equal(fim, vigencia.DataFim);
    }

    [Theory]
    [MemberData(nameof(DatasInvalidas))]
    public void Criar_Com_Fim_Igual_Ou_Anterior_Ao_Inicio_Lanca_DomainException(DateOnly inicio, DateOnly fim)
    {
        Assert.Throws<DomainException>(() => Vigencia.Criar(inicio, fim));
    }

    public static IEnumerable<object[]> DatasInvalidas()
    {
        yield return [new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 1)];
        yield return [new DateOnly(2026, 6, 1), new DateOnly(2026, 1, 1)];
    }

    [Fact]
    public void CriarPadrao_Retorna_Vigencia_De_Um_Ano()
    {
        var dataContratacao = new DateOnly(2026, 1, 1);

        var vigencia = Vigencia.CriarPadrao(dataContratacao);

        Assert.Equal(dataContratacao, vigencia.DataInicio);
        Assert.Equal(dataContratacao.AddDays(365), vigencia.DataFim);
    }
}


