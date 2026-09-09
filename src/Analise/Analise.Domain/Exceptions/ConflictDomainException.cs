namespace Analise.Domain.Exceptions;

/// <summary>Raised when an operation conflicts with the current state of the aggregate. Adapters map this to HTTP 409.</summary>
public sealed class ConflictDomainException : DomainException
{
    public ConflictDomainException(string message)
        : base(message)
    {
    }
}
