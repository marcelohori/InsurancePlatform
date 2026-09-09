using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Contratacao.Infrastructure.Persistence;

/// <summary>Used only by "dotnet ef migrations" at design time - see Proposta's equivalent factory for rationale.</summary>
public sealed class ContratacaoDbContextFactory : IDesignTimeDbContextFactory<ContratacaoDbContext>
{
    public ContratacaoDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ContratacaoDbContext>();
        var connectionString = Environment.GetEnvironmentVariable("CONTRATACAO_DB_CONNECTION")
            ?? "Host=localhost;Port=5432;Database=contratacao_db;Username=postgres;Password=postgres";

        optionsBuilder.UseNpgsql(connectionString, npgsqlOptions =>
        {
            npgsqlOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), errorCodesToAdd: null);
            npgsqlOptions.CommandTimeout(30);
        });

        return new ContratacaoDbContext(optionsBuilder.Options);
    }
}
