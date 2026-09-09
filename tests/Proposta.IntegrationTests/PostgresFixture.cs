using Microsoft.EntityFrameworkCore;
using Proposta.Infrastructure.Persistence;
using Testcontainers.PostgreSql;
using Xunit;

namespace Proposta.IntegrationTests;

/// <summary>
/// Starts a real Postgres container once per test collection, applies the EF Core
/// migrations, and hands out a fresh DbContext per test.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("proposta_db")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        await using var dbContext = CreateDbContext();
        await dbContext.Database.MigrateAsync();
    }

    public PropostaDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<PropostaDbContext>()
            .UseNpgsql(_container.GetConnectionString())
            .Options;

        return new PropostaDbContext(options);
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();
}

[CollectionDefinition(nameof(PostgresCollection))]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>;


