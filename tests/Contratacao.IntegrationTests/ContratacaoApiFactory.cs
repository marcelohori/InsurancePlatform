using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Contratacao.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using WireMock.Server;
using Xunit;

namespace Contratacao.IntegrationTests;

/// <summary>
/// Boots Contratacao.Api against real Postgres and RabbitMQ containers, with a WireMock stand-in
/// for Proposta.Api - see PostgresFixture.cs / HttpPropostaVerificationAdapterResilienceTests.cs
/// in this same project for the "real DB" and "real broker unavailable" scenarios respectively;
/// this factory focuses on the business flow that depends on Proposta.Api's response.
///
/// Configuration is injected via environment variables for the same reason as Proposta.Api's
/// own factory: Contratacao.Infrastructure reads configuration eagerly at service-registration
/// time, before WebApplicationFactory's ConfigureAppConfiguration override would be visible.
/// </summary>
public sealed class ContratacaoApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string JwtSigningKey = "integration-test-signing-key-at-least-32-bytes-long";
    public const string JwtIssuer = "InsurancePlatformV01.Tests";
    public const string JwtAudience = "InsurancePlatformV01.Tests";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("contratacao_db")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private readonly RabbitMqContainer _rabbitMq = new RabbitMqBuilder("rabbitmq:3-management-alpine")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    private WireMockServer? _propostaApiStub;

    public WireMockServer PropostaApiStub => _propostaApiStub ?? throw new InvalidOperationException("Factory not initialized.");

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        await _rabbitMq.StartAsync();
        _propostaApiStub = WireMockServer.Start();

        Environment.SetEnvironmentVariable("ConnectionStrings__ContratacaoDb", _postgres.GetConnectionString());
        Environment.SetEnvironmentVariable("RabbitMq__Host", _rabbitMq.Hostname);
        Environment.SetEnvironmentVariable("RabbitMq__Port", _rabbitMq.GetMappedPublicPort(5672).ToString(CultureInfo.InvariantCulture));
        Environment.SetEnvironmentVariable("RabbitMq__Username", "test");
        Environment.SetEnvironmentVariable("RabbitMq__Password", "test");
        Environment.SetEnvironmentVariable("PropostaApi__BaseUrl", _propostaApiStub.Url);
        Environment.SetEnvironmentVariable("Jwt__Issuer", JwtIssuer);
        Environment.SetEnvironmentVariable("Jwt__Audience", JwtAudience);
        Environment.SetEnvironmentVariable("Jwt__SigningKey", JwtSigningKey);

        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ContratacaoDbContext>();
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
        _propostaApiStub?.Stop();
        _propostaApiStub?.Dispose();
        await _postgres.DisposeAsync();
        await _rabbitMq.DisposeAsync();
        await base.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    Task IAsyncLifetime.DisposeAsync() => DisposeAsync().AsTask();
}

[CollectionDefinition(nameof(ContratacaoApiCollection))]
public sealed class ContratacaoApiCollection : ICollectionFixture<ContratacaoApiFactory>;

