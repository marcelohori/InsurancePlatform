using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Proposta.Infrastructure.Persistence;

/// <summary>
/// Used only by "dotnet ef migrations" at design time - decouples the EF tooling from a
/// fully wired Api host. Runtime configuration is resolved by ServiceCollectionExtensions.
/// </summary>
public sealed class PropostaDbContextFactory : IDesignTimeDbContextFactory<PropostaDbContext>
{
    public PropostaDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<PropostaDbContext>();
        var connectionString = Environment.GetEnvironmentVariable("PROPOSTA_DB_CONNECTION")
            ?? "Host=localhost;Port=5432;Database=proposta_db;Username=postgres;Password=postgres";

        optionsBuilder.UseNpgsql(connectionString, npgsqlOptions =>
        {
            npgsqlOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), errorCodesToAdd: null);
            npgsqlOptions.CommandTimeout(30);
        });

        return new PropostaDbContext(optionsBuilder.Options);
    }
}
