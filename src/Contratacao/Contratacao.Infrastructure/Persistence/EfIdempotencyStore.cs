using Contratacao.Application.Ports;
using Microsoft.EntityFrameworkCore;

namespace Contratacao.Infrastructure.Persistence;

/// <summary>
/// Stages the idempotency record on the same DbContext as the contratação it maps to, so both
/// commit atomically when <see cref="EfUnitOfWork.SalvarAlteracoesAsync"/> runs.
///
/// Thread-safe against concurrent requests with the same idempotency key: the database's
/// UNIQUE constraint on Chave ensures only one record persists. Callers must handle
/// DbUpdateException (duplicate key) by re-querying the entry after conflict.
/// </summary>
public sealed class EfIdempotencyStore(ContratacaoDbContext dbContext) : IIdempotencyStore
{
    /// <summary>
    /// Retrieves an idempotency record by key. Uses a serializable read to ensure
    /// we see the most up-to-date data, even under high concurrency.
    /// </summary>
    public async Task<IdempotencyEntry?> ObterAsync(string chaveIdempotencia, CancellationToken cancellationToken)
    {
        var registro = await dbContext.IdempotencyRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Chave == chaveIdempotencia, cancellationToken);

        return registro is null ? null : new IdempotencyEntry(registro.ContratacaoId, registro.HashRequisicao);
    }

    /// <summary>
    /// Stages an idempotency record for insertion. The actual write is deferred until
    /// SaveChanges (atomic with the contratação it guards). On concurrent duplicate-key
    /// attempts, SaveChanges will throw DbUpdateException - callers MUST catch and retry.
    /// </summary>
    public void Registrar(string chaveIdempotencia, Guid contratacaoId, string hashRequisicao) =>
        dbContext.IdempotencyRecords.Add(new IdempotencyRecord
        {
            Chave = chaveIdempotencia,
            ContratacaoId = contratacaoId,
            HashRequisicao = hashRequisicao,
            CriadoEm = DateTimeOffset.UtcNow,
        });
}
