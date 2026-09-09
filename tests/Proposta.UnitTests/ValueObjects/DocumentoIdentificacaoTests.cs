using Proposta.Domain.Exceptions;
using Proposta.Domain.ValueObjects;
using Xunit;

namespace Proposta.UnitTests.ValueObjects;

public class DocumentoIdentificacaoTests
{
    [Theory]
    [InlineData("111.444.777-35")]
    [InlineData("11144477735")]
    public void Criar_Com_Cpf_Valido_Retorna_Documento(string valor)
    {
        var documento = DocumentoIdentificacao.Criar(valor);

        Assert.Equal(TipoDocumento.Cpf, documento.Tipo);
        Assert.Equal("11144477735", documento.Numero);
    }

    [Theory]
    [InlineData("11.222.333/0001-81")]
    [InlineData("11222333000181")]
    public void Criar_Com_Cnpj_Valido_Retorna_Documento(string valor)
    {
        var documento = DocumentoIdentificacao.Criar(valor);

        Assert.Equal(TipoDocumento.Cnpj, documento.Tipo);
        Assert.Equal("11222333000181", documento.Numero);
    }

    [Theory]
    [InlineData("11111111111")]
    [InlineData("11144477736")]
    [InlineData("123")]
    [InlineData("")]
    public void Criar_Com_Documento_Invalido_Lanca_DomainException(string valor)
    {
        Assert.Throws<DomainException>(() => DocumentoIdentificacao.Criar(valor));
    }
}


