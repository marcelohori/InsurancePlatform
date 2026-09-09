using Microsoft.EntityFrameworkCore;
using Proposta.Application.Ports;
using Proposta.Domain;

namespace Proposta.Infrastructure.Persistence;

public sealed class EfPropostaRepository(PropostaDbContext dbContext) : IPropostaRepository
{
    public void Adicionar(PropostaSeguro proposta) => dbContext.Propostas.Add(proposta);

    public async Task<PropostaSeguro?> ObterPorIdAsync(Guid id, string? criadoPor, CancellationToken cancellationToken) =>
        await dbContext.Propostas
            .AsNoTracking()
            .FirstOrDefaultAsync(
            p => p.Id == id && (criadoPor == null || p.CriadoPor == criadoPor), cancellationToken);

    public async Task<IReadOnlyList<PropostaSeguro>> ListarAsync(int pagina, int tamanhoPagina, string? criadoPor, CancellationToken cancellationToken) =>
        await dbContext.Propostas
            .AsNoTracking()
            .Where(p => criadoPor == null || p.CriadoPor == criadoPor)
            .OrderBy(p => p.DataCriacao)
            .ThenBy(p => p.Id)
            .Skip((pagina - 1) * tamanhoPagina)
            .Take(tamanhoPagina)
            .ToListAsync(cancellationToken);

    public void Atualizar(PropostaSeguro proposta) => dbContext.Propostas.Update(proposta);

    public void Remover(PropostaSeguro proposta) => dbContext.Propostas.Remove(proposta);
}
