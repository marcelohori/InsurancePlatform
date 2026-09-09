using Contratacao.Domain;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Contratacao.Infrastructure.Persistence;

public sealed class ContratacaoDbContext(DbContextOptions<ContratacaoDbContext> options) : DbContext(options)
{
    public DbSet<ApoliceSeguro> Contratacoes => Set<ApoliceSeguro>();

    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ContratacaoDbContext).Assembly);

        modelBuilder.AddTransactionalOutboxEntities();
    }
}
