using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Proposta.Api.Endpoints;

/// <summary>
/// Dev-only helper to mint JWTs for manual testing (Postman, curl) without a real login flow.
/// Mapped only when the host is running in the Development environment - see Program.cs. The
/// signing key/issuer/audience are shared across all three services, so a token minted here is
/// valid on Proposta.Api, Contratacao.Api and Analise.Api alike.
/// </summary>
public static class DevAuthEndpoints
{
    private static readonly string[] PapeisValidos = ["usuario", "analista", "admin"];

    public static void MapDevAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/dev/auth/token", (GerarTokenRequest? request, IConfiguration configuration) =>
        {
            var usuarioId = string.IsNullOrWhiteSpace(request?.UsuarioId) ? "dev-user" : request.UsuarioId;
            var papel = string.IsNullOrWhiteSpace(request?.Role) ? "usuario" : request.Role;

            if (!PapeisValidos.Contains(papel, StringComparer.OrdinalIgnoreCase))
            {
                return Results.BadRequest(new { error = $"Role inválida. Valores aceitos: {string.Join(", ", PapeisValidos)}" });
            }

            var signingKey = configuration["Jwt:SigningKey"]!;
            var issuer = configuration["Jwt:Issuer"];
            var audience = configuration["Jwt:Audience"];

            var credentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
                SecurityAlgorithms.HmacSha256);

            var expiraEm = DateTime.UtcNow.AddHours(1);
            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims:
                [
                    new Claim(ClaimTypes.Role, papel),
                    new Claim(ClaimTypes.NameIdentifier, usuarioId),
                    new Claim(ClaimTypes.Name, usuarioId),
                ],
                expires: expiraEm,
                signingCredentials: credentials);

            return Results.Ok(new
            {
                token = new JwtSecurityTokenHandler().WriteToken(token),
                usuarioId,
                role = papel,
                expiresAt = expiraEm,
            });
        })
        .WithName("GerarTokenDev")
        .WithSummary("[DEV ONLY] Gera um JWT de teste para o papel/usuário informado.")
        .WithTags("Dev");
    }

    private sealed record GerarTokenRequest(string? UsuarioId, string? Role);
}
