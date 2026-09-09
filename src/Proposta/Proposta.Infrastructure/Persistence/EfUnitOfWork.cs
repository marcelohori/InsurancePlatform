using Proposta.Application.Ports;

namespace Proposta.Infrastructure.Persistence;

public sealed class EfUnitOfWork(PropostaDbContext dbContext) : IUnitOfWork
{
    public Task SalvarAlteracoesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
