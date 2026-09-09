namespace Analise.Domain.Exceptions;

/// <summary>Raised when a domain invariant is violated. Adapters translate this into the service's Problem Details error contract.</summary>
public class DomainException : Exception
{
    public DomainException(string message)
        : base(message)
    {
    }
}
