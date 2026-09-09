namespace BuildingBlocks.Contracts.Authorization;

/// <summary>
/// Centralized authorization policy definitions for the InsurancePlatform.
/// Ensures consistent role-based access control across all services.
/// Policy names should be used in controllers like: [Authorize(Policy = AuthorizationPolicies.PropostaCriar)]
/// </summary>
public static class AuthorizationPolicies
{
    public const string PropostaCriar = nameof(PropostaCriar);
    public const string PropostaGerenciar = nameof(PropostaGerenciar);
    public const string ContratacaoCriar = nameof(ContratacaoCriar);
}
