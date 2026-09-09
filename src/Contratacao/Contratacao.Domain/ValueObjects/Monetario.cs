using Contratacao.Domain.Exceptions;

namespace Contratacao.Domain.ValueObjects;

/// <summary>
/// Immutable money value object: an amount paired with an ISO 4217 currency code.
/// Deliberately duplicated from Proposta.Domain rather than shared - keeps the two bounded
/// contexts independently deployable and versionable (see design.md decision 13).
/// </summary>
public sealed class Monetario : IEquatable<Monetario>
{
    public const string MoedaPadrao = "BRL";

    public decimal Quantia { get; }

    public string Moeda { get; }

    private Monetario(decimal quantia, string moeda)
    {
        Quantia = quantia;
        Moeda = moeda;
    }

    public static Monetario Criar(decimal quantia, string moeda = MoedaPadrao)
    {
        if (quantia < 0 || decimal.Round(quantia, 2) != quantia)
        {
            throw new DomainException("O valor monetário deve ser não negativo e ter no máximo duas casas decimais.");
        }

        if (string.IsNullOrWhiteSpace(moeda)
            || moeda.Length != 3
            || moeda.Any(caractere => !((caractere >= 'A' && caractere <= 'Z') || (caractere >= 'a' && caractere <= 'z'))))
        {
            throw new DomainException($"Código de moeda inválido: '{moeda}'.");
        }

        return new Monetario(quantia, moeda.ToUpperInvariant());
    }

    public bool Equals(Monetario? other) =>
        other is not null && Quantia == other.Quantia && Moeda == other.Moeda;

    public override bool Equals(object? obj) => Equals(obj as Monetario);

    public override int GetHashCode() => HashCode.Combine(Quantia, Moeda);

    public override string ToString() => $"{Moeda} {Quantia:0.00}";
}
