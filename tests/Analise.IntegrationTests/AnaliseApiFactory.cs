using System.Globalization;
using Analise.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using WireMock.Server;
using Xunit;

namespace Analise.IntegrationTests;

/// <summary>
/// Boots Analise.Api against real Postgres and RabbitMQ containers, with a WireMock stand-in
/// for the Anthropic API. Configuration goes through environment variables for the same reason
/// as Proposta.Api/Contratacao.Api's own factories (eager config read at registration time).
/// </summary>
public sealed class AnaliseApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string JwtSigningKey = "integration-test-signing-key-at-least-32-bytes-long";
    public const string JwtIssuer = "InsurancePlatformV01.Tests";
    public const string JwtAudience = "InsurancePlatformV01.Tests";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("analise_db")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private readonly RabbitMqContainer _rabbitMq = new RabbitMqBuilder("rabbitmq:3-management-alpine")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    private WireMockServer? _anthropicStub;

    public WireMockServer AnthropicStub => _anthropicStub ?? throw new InvalidOperationException("Factory not initialized.");

    public string RabbitMqHost => _rabbitMq.Hostname;

    public ushort RabbitMqPort => _rabbitMq.GetMappedPublicPort(5672);

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        await _rabbitMq.StartAsync();
        _anthropicStub = WireMockServer.Start();

        Environment.SetEnvironmentVariable("ConnectionStrings__AnaliseDb", _postgres.GetConnectionString());
        Environment.SetEnvironmentVariable("RabbitMq__Host", _rabbitMq.Hostname);
        Environment.SetEnvironmentVariable("RabbitMq__Port", RabbitMqPort.ToString(CultureInfo.InvariantCulture));
        Environment.SetEnvironmentVariable("RabbitMq__Username", "test");
        Environment.SetEnvironmentVariable("RabbitMq__Password", "test");
        Environment.SetEnvironmentVariable("Anthropic__BaseUrl", _anthropicStub.Url);
        Environment.SetEnvironmentVariable("Anthropic__AllowInsecureHttpForDevelopment", "true");
        Environment.SetEnvironmentVariable("Anthropic__AllowInsecureHttpForDevelopment", "true");
        Environment.SetEnvironmentVariable("Anthropic__ApiKey", "test-key");
        Environment.SetEnvironmentVariable("Anthropic__Model", "claude-haiku-4-5-20251001");
        Environment.SetEnvironmentVariable("Jwt__Issuer", JwtIssuer);
        Environment.SetEnvironmentVariable("Jwt__Audience", JwtAudience);
        Environment.SetEnvironmentVariable("Jwt__SigningKey", JwtSigningKey);

        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AnaliseDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    public override async ValueTask DisposeAsync()
    {
        _anthropicStub?.Stop();
        _anthropicStub?.Dispose();
        await _postgres.DisposeAsync();
        await _rabbitMq.DisposeAsync();
        await base.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    Task IAsyncLifetime.DisposeAsync() => DisposeAsync().AsTask();
}

[CollectionDefinition(nameof(AnaliseApiCollection))]
public sealed class AnaliseApiCollection : ICollectionFixture<AnaliseApiFactory>;

