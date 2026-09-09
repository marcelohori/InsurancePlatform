using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Analise.Infrastructure.Persistence;

/// <summary>Used only by "dotnet ef migrations" at design time - see Proposta's equivalent factory for rationale.</summary>
public sealed class AnaliseDbContextFactory : IDesignTimeDbContextFactory<AnaliseDbContext>
{
    public AnaliseDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AnaliseDbContext>();
        var connectionString = Environment.GetEnvironmentVariable("ANALISE_DB_CONNECTION")
            ?? "Host=localhost;Port=5432;Database=analise_db;Username=postgres;Password=postgres";

        optionsBuilder.UseNpgsql(connectionString, npgsqlOptions =>
        {
            npgsqlOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), errorCodesToAdd: null);
            npgsqlOptions.CommandTimeout(30);
        });

        return new AnaliseDbContext(optionsBuilder.Options);
    }
}
