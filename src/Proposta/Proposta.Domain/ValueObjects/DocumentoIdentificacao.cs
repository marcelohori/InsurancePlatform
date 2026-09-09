using System.Text.RegularExpressions;
using Proposta.Domain.Exceptions;

namespace Proposta.Domain.ValueObjects;

/// <summary>
/// CPF or CNPJ, immutable and always valid once constructed - the digits are checked
/// against the official check-digit algorithm at construction time.
/// </summary>
public sealed partial class DocumentoIdentificacao : IEquatable<DocumentoIdentificacao>
{
    public string Numero { get; }

    public TipoDocumento Tipo { get; }

    private DocumentoIdentificacao(string numero, TipoDocumento tipo)
    {
        Numero = numero;
        Tipo = tipo;
    }

    public static DocumentoIdentificacao Criar(string valor)
    {
        var digitos = SomenteDigitosRegex().Replace(valor ?? string.Empty, string.Empty);

        return digitos.Length switch
        {
            11 when EhCpfValido(digitos) => new DocumentoIdentificacao(digitos, TipoDocumento.Cpf),
            14 when EhCnpjValido(digitos) => new DocumentoIdentificacao(digitos, TipoDocumento.Cnpj),
            _ => throw new DomainException($"Documento de identificação inválido: '{valor}'."),
        };
    }

    private static bool EhCpfValido(string cpf)
    {
        if (TodosDigitosIguais(cpf))
        {
            return false;
        }

        var digito1 = CalcularDigitoVerificador(cpf, 9, [10, 9, 8, 7, 6, 5, 4, 3, 2]);
        var digito2 = CalcularDigitoVerificador(cpf, 10, [11, 10, 9, 8, 7, 6, 5, 4, 3, 2]);

        return cpf[9] - '0' == digito1 && cpf[10] - '0' == digito2;
    }

    private static bool EhCnpjValido(string cnpj)
    {
        if (TodosDigitosIguais(cnpj))
        {
            return false;
        }

        var digito1 = CalcularDigitoVerificador(cnpj, 12, [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2]);
        var digito2 = CalcularDigitoVerificador(cnpj, 13, [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2]);

        return cnpj[12] - '0' == digito1 && cnpj[13] - '0' == digito2;
    }

    private static int CalcularDigitoVerificador(string documento, int quantidadeDigitos, int[] pesos)
    {
        var soma = 0;
        for (var i = 0; i < quantidadeDigitos; i++)
        {
            soma += (documento[i] - '0') * pesos[i];
        }

        var resto = soma % 11;
        return resto < 2 ? 0 : 11 - resto;
    }

    private static bool TodosDigitosIguais(string documento) => documento.Distinct().Count() == 1;

    public bool Equals(DocumentoIdentificacao? other) =>
        other is not null && Numero == other.Numero && Tipo == other.Tipo;

    public override bool Equals(object? obj) => Equals(obj as DocumentoIdentificacao);

    public override int GetHashCode() => HashCode.Combine(Numero, Tipo);

    public override string ToString() => Numero;

    /// <summary>
    /// Returns the document in human-readable format with separators:
    /// CPF: 123.456.789-01 | CNPJ: 12.345.678/0001-90
    /// </summary>
    public string ObterFormatado() =>
        Tipo switch
        {
            TipoDocumento.Cpf => $"{Numero[..3]}.{Numero[3..6]}.{Numero[6..9]}-{Numero[9..]}",
            TipoDocumento.Cnpj => $"{Numero[..2]}.{Numero[2..5]}.{Numero[5..8]}/{Numero[8..12]}-{Numero[12..]}",
            _ => Numero,
        };

    [GeneratedRegex(@"\D")]
    private static partial Regex SomenteDigitosRegex();
}

public enum TipoDocumento
{
    Cpf,
    Cnpj,
}
