using Contratacao.Application.Ports;
using Contratacao.Domain;
using Microsoft.EntityFrameworkCore;

namespace Contratacao.Infrastructure.Persistence;

public sealed class EfContratacaoRepository(ContratacaoDbContext dbContext) : IContratacaoRepository
{
    public void Adicionar(ApoliceSeguro apolice) => dbContext.Contratacoes.Add(apolice);

    public async Task<ApoliceSeguro?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken) =>
        await dbContext.Contratacoes
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public async Task<IReadOnlyList<ApoliceSeguro>> ListarAsync(int pagina, int tamanhoPagina, CancellationToken cancellationToken) =>
        await dbContext.Contratacoes
            .AsNoTracking()
            .OrderBy(a => a.DataContratacao)
            .ThenBy(a => a.Id)
            .Skip((pagina - 1) * tamanhoPagina)
            .Take(tamanhoPagina)
            .ToListAsync(cancellationToken);

    public void Atualizar(ApoliceSeguro apolice) => dbContext.Contratacoes.Update(apolice);

    public void Remover(ApoliceSeguro apolice) => dbContext.Contratacoes.Remove(apolice);
}
