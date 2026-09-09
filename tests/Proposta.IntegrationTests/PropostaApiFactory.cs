using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Proposta.Infrastructure.Persistence;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Xunit;

namespace Proposta.IntegrationTests;

/// <summary>
/// Boots Proposta.Api against real Postgres and RabbitMQ containers, so the CRUD + outbox
/// event flow is exercised exactly as it runs in production - no mocked infrastructure.
///
/// Configuration is injected via environment variables (not WebApplicationFactory's
/// ConfigureAppConfiguration) because Proposta.Infrastructure reads the connection string
/// eagerly, at service-registration time in Program.cs - before ConfigureAppConfiguration's
/// override would be visible. Environment variables are already part of the process by the
/// time the host builds, so they are seen no matter when that registration code runs.
/// </summary>
public sealed class PropostaApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string JwtSigningKey = "integration-test-signing-key-at-least-32-bytes-long";
    public const string JwtIssuer = "InsurancePlatformV01.Tests";
    public const string JwtAudience = "InsurancePlatformV01.Tests";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("proposta_db")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private readonly RabbitMqContainer _rabbitMq = new RabbitMqBuilder("rabbitmq:3-management-alpine")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        await _rabbitMq.StartAsync();

        Environment.SetEnvironmentVariable("ConnectionStrings__PropostaDb", _postgres.GetConnectionString());
        Environment.SetEnvironmentVariable("RabbitMq__Host", _rabbitMq.Hostname);
        Environment.SetEnvironmentVariable("RabbitMq__Port", RabbitMqPort.ToString(CultureInfo.InvariantCulture));
        Environment.SetEnvironmentVariable("RabbitMq__Username", "test");
        Environment.SetEnvironmentVariable("RabbitMq__Password", "test");
        Environment.SetEnvironmentVariable("Jwt__Issuer", JwtIssuer);
        Environment.SetEnvironmentVariable("Jwt__Audience", JwtAudience);
        Environment.SetEnvironmentVariable("Jwt__SigningKey", JwtSigningKey);

        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PropostaDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    public string RabbitMqHost => _rabbitMq.Hostname;

    public ushort RabbitMqPort => _rabbitMq.GetMappedPublicPort(5672);

    /// <summary>
    /// Generates a valid JWT token for integration testing with the given subject (user id).
    /// </summary>
    public static string GerarTokenAcesso(string subject, string? role = null)
    {
        var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(JwtSigningKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, subject),
            new(ClaimTypes.Name, subject),
        };

        if (!string.IsNullOrEmpty(role))
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var token = new JwtSecurityToken(
            issuer: JwtIssuer,
            audience: JwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public override async ValueTask DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await _rabbitMq.DisposeAsync();
        await base.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    Task IAsyncLifetime.DisposeAsync() => DisposeAsync().AsTask();
}

[CollectionDefinition(nameof(PropostaApiCollection))]
public sealed class PropostaApiCollection : ICollectionFixture<PropostaApiFactory>;


