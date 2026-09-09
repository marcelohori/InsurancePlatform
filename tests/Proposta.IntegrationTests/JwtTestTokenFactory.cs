using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Proposta.IntegrationTests;

public static class JwtTestTokenFactory
{
    public static string CriarToken(string papel = "Analista", string usuarioId = "test-user")
    {
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(PropostaApiFactory.JwtSigningKey));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: PropostaApiFactory.JwtIssuer,
            audience: PropostaApiFactory.JwtAudience,
            claims: [new Claim(ClaimTypes.Role, papel), new Claim(ClaimTypes.NameIdentifier, usuarioId), new Claim(ClaimTypes.Name, usuarioId)],
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}


