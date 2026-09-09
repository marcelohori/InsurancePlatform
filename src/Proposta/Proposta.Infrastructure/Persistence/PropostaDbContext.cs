using MassTransit;
using Microsoft.EntityFrameworkCore;
using Proposta.Domain;

namespace Proposta.Infrastructure.Persistence;

public sealed class PropostaDbContext(DbContextOptions<PropostaDbContext> options) : DbContext(options)
{
    public DbSet<PropostaSeguro> Propostas => Set<PropostaSeguro>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PropostaDbContext).Assembly);

        // Transactional outbox tables (MassTransit publishes only after SaveChanges commits).
        modelBuilder.AddTransactionalOutboxEntities();
    }
}
