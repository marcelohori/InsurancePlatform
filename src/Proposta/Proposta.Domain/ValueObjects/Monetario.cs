using Proposta.Domain.Exceptions;

namespace Proposta.Domain.ValueObjects;

/// <summary>
/// Immutable money value object: an amount paired with an ISO 4217 currency code.
/// Arithmetic never mutates an instance - it always returns a new one.
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

    public Monetario Somar(Monetario outro)
    {
        GarantirMesmaMoeda(outro);
        return new Monetario(Quantia + outro.Quantia, Moeda);
    }

    public Monetario Subtrair(Monetario outro)
    {
        GarantirMesmaMoeda(outro);
        return Criar(Quantia - outro.Quantia, Moeda);
    }

    private void GarantirMesmaMoeda(Monetario outro)
    {
        if (Moeda != outro.Moeda)
        {
            throw new DomainException($"Não é possível operar valores em moedas diferentes ('{Moeda}' e '{outro.Moeda}').");
        }
    }

    public bool Equals(Monetario? other) =>
        other is not null && Quantia == other.Quantia && Moeda == other.Moeda;

    public override bool Equals(object? obj) => Equals(obj as Monetario);

    public override int GetHashCode() => HashCode.Combine(Quantia, Moeda);

    public override string ToString() => $"{Moeda} {Quantia:0.00}";
}
