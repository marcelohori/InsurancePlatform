namespace BuildingBlocks.Contracts.Errors;

/// <summary>
/// Shared "type" URIs used across the three services so every Problem Details (RFC 7807)
/// response follows the same error contract, regardless of which service produced it.
/// </summary>
public static class ProblemTypes
{
    private const string Base = "https://insuranceplatform.dev/problems/";

    public const string ValidationError = Base + "validation-error";
    public const string NotFound = Base + "not-found";
    public const string Conflict = Base + "conflict";
    public const string ServiceUnavailable = Base + "service-unavailable";
}
