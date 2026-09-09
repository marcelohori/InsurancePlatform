using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Analise.IntegrationTests;

public static class JwtTestTokenFactory
{
    public static string CriarToken()
    {
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(AnaliseApiFactory.JwtSigningKey));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: AnaliseApiFactory.JwtIssuer,
            audience: AnaliseApiFactory.JwtAudience,
            claims: [new Claim(ClaimTypes.Name, "test-user")],
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

