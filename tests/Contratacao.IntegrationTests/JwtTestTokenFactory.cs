using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Contratacao.IntegrationTests;

public static class JwtTestTokenFactory
{
    public static string CriarToken(string papel = "Analista")
    {
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(ContratacaoApiFactory.JwtSigningKey));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: ContratacaoApiFactory.JwtIssuer,
            audience: ContratacaoApiFactory.JwtAudience,
            claims: [new Claim(ClaimTypes.Role, papel), new Claim(ClaimTypes.Name, "test-user")],
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}


