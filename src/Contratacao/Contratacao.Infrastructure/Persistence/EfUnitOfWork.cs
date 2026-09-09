using Contratacao.Application.Ports;
using Contratacao.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Contratacao.Infrastructure.Persistence;

public sealed class EfUnitOfWork(ContratacaoDbContext dbContext) : IUnitOfWork
{
    public async Task SalvarAlteracoesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (EhConflitoDeIdempotencia(ex))
        {
            throw new Contratacao.Domain.Exceptions.ConflictDomainException(
                "A chave de idempotência já está sendo processada.");
        }
    }

    private static bool EhConflitoDeIdempotencia(DbUpdateException exception) =>
        exception.InnerException is PostgresException postgresException
        && postgresException.SqlState == PostgresErrorCodes.UniqueViolation
        && string.Equals(postgresException.ConstraintName, "PK_idempotency_records", StringComparison.Ordinal);
}
