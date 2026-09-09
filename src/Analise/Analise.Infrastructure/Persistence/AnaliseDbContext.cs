using Analise.Domain;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Analise.Infrastructure.Persistence;

public sealed class AnaliseDbContext(DbContextOptions<AnaliseDbContext> options) : DbContext(options)
{
    public DbSet<AnaliseRisco> Analises => Set<AnaliseRisco>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AnaliseDbContext).Assembly);

        // Outbox is unused here (Analise.Api publishes nothing today) but AddTransactionalOutboxEntities
        // also creates the InboxState table, which the PropostaCriadaEvent consumer needs for dedup.
        modelBuilder.AddTransactionalOutboxEntities();
    }
}
