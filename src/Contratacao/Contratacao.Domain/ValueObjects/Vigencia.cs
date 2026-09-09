using Contratacao.Domain.Exceptions;

namespace Contratacao.Domain.ValueObjects;

/// <summary>
/// Immutable coverage period. <see cref="DataFim"/> is always strictly after
/// <see cref="DataInicio"/> - enforced at construction, never in the caller.
/// </summary>
public sealed class Vigencia : IEquatable<Vigencia>
{
    /// <summary>
    /// Default coverage duration: 365 days (1 year). Used by CriarPadrao()
    /// when no explicit coverage period is specified during contract creation.
    /// </summary>
    public const int DiacasVigenciaPadrao = 365;

    private static readonly TimeSpan DuracaoPadrao = TimeSpan.FromDays(DiacasVigenciaPadrao);

    public DateOnly DataInicio { get; }

    public DateOnly DataFim { get; }

    private Vigencia(DateOnly dataInicio, DateOnly dataFim)
    {
        DataInicio = dataInicio;
        DataFim = dataFim;
    }

    public static Vigencia Criar(DateOnly dataInicio, DateOnly dataFim)
    {
        if (dataFim <= dataInicio)
        {
            throw new DomainException("A data de fim de vigência deve ser estritamente posterior à data de início.");
        }

        return new Vigencia(dataInicio, dataFim);
    }

    /// <summary>1 year from the contract date, per the default coverage rule.</summary>
    public static Vigencia CriarPadrao(DateOnly dataContratacao) =>
        new(dataContratacao, dataContratacao.AddDays(DuracaoPadrao.Days));

    public bool Equals(Vigencia? other) =>
        other is not null && DataInicio == other.DataInicio && DataFim == other.DataFim;

    public override bool Equals(object? obj) => Equals(obj as Vigencia);

    public override int GetHashCode() => HashCode.Combine(DataInicio, DataFim);
}
