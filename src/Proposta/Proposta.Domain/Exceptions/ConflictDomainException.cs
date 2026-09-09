namespace Proposta.Domain.Exceptions;

/// <summary>
/// Raised when an operation conflicts with the current state of the aggregate
/// (e.g. trying to change a status that is already final). Adapters map this to HTTP 409.
/// </summary>
public sealed class ConflictDomainException : DomainException
{
    public ConflictDomainException(string message)
        : base(message)
    {
    }
}
