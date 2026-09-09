using System.Globalization;
using Analise.Application.DependencyInjection;
using Analise.Infrastructure.DependencyInjection;
using Analise.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using WireMock.Server;
using Xunit;

namespace Analise.IntegrationTests;

/// <summary>
/// Boots the Analise.Infrastructure/Application composition (no Api layer yet - that's task 4.4)
/// as a real generic host, against real Postgres and RabbitMQ containers, with a WireMock
/// stand-in for the Anthropic API.
/// </summary>
public sealed class AnaliseProcessingFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("analise_db")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private readonly RabbitMqContainer _rabbitMq = new RabbitMqBuilder("rabbitmq:3-management-alpine")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    private IHost? _host;

    public WireMockServer AnthropicStub { get; private set; } = null!;

    public IServiceProvider Services => _host!.Services;

    public string RabbitMqHost => _rabbitMq.Hostname;

    public ushort RabbitMqPort => _rabbitMq.GetMappedPublicPort(5672);

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        await _rabbitMq.StartAsync();
        AnthropicStub = WireMockServer.Start();

        var testSettings = new Dictionary<string, string?>
        {
            ["ConnectionStrings:AnaliseDb"] = _postgres.GetConnectionString(),
            ["RabbitMq:Host"] = _rabbitMq.Hostname,
            ["RabbitMq:Port"] = _rabbitMq.GetMappedPublicPort(5672).ToString(CultureInfo.InvariantCulture),
            ["RabbitMq:Username"] = "test",
            ["RabbitMq:Password"] = "test",
            ["Anthropic:BaseUrl"] = AnthropicStub.Url,
            ["Anthropic:ApiKey"] = "test-key",
            ["Anthropic:Model"] = "claude-haiku-4-5-20251001",
        };

        // Feed settings through ConfigureAppConfiguration (not a standalone ConfigurationBuilder)
        // so the SAME IConfiguration instance is what gets registered in DI - AnthropicRiskAssessmentAdapter
        // injects IConfiguration directly, and a separately-built object would not reach it.
        _host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration(configBuilder => configBuilder.AddInMemoryCollection(testSettings))
            .ConfigureServices((context, services) =>
            {
                services.AddAnaliseInfrastructure(context.Configuration).AddAnaliseApplication();
            })
            .Build();

        using (var scope = _host.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AnaliseDbContext>();
            await dbContext.Database.MigrateAsync();
        }

        await _host.StartAsync();
    }

    public async Task DisposeAsync()
    {
        if (_host is not null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }

        AnthropicStub.Stop();
        AnthropicStub.Dispose();
        await _postgres.DisposeAsync();
        await _rabbitMq.DisposeAsync();
    }
}

[CollectionDefinition(nameof(AnaliseProcessingCollection))]
public sealed class AnaliseProcessingCollection : ICollectionFixture<AnaliseProcessingFixture>;

