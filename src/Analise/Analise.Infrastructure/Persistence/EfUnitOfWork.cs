using Analise.Application.Ports;

namespace Analise.Infrastructure.Persistence;

public sealed class EfUnitOfWork(AnaliseDbContext dbContext) : IUnitOfWork
{
    public Task SalvarAlteracoesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
