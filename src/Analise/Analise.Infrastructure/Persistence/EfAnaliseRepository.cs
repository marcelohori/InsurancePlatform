using Analise.Application.Ports;
using Analise.Domain;
using Microsoft.EntityFrameworkCore;

namespace Analise.Infrastructure.Persistence;

public sealed class EfAnaliseRepository(AnaliseDbContext dbContext) : IAnaliseRepository
{
    public void Adicionar(AnaliseRisco analise) => dbContext.Analises.Add(analise);

    public async Task<AnaliseRisco?> ObterPorPropostaIdAsync(Guid propostaId, CancellationToken cancellationToken) =>
        await dbContext.Analises
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.PropostaId == propostaId, cancellationToken);

    public void Atualizar(AnaliseRisco analise) => dbContext.Analises.Update(analise);
}
