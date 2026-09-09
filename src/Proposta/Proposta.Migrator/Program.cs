using Microsoft.EntityFrameworkCore;
using Npgsql;
using Proposta.Infrastructure.Persistence;

// Fixed per-service id for pg_advisory_lock - advisory locks are scoped to the connected
// database, so this only needs to be unique within proposta_db, not across services.
const long AdvisoryLockId = 721_001;

var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__PropostaDbMigrator")
    ?? Environment.GetEnvironmentVariable("ConnectionStrings__PropostaDb")
    ?? throw new InvalidOperationException(
        "Connection string não configurada. Defina 'ConnectionStrings__PropostaDbMigrator' com um usuário que tenha permissão de DDL no schema.");

await using var lockConnection = new NpgsqlConnection(connectionString);
await lockConnection.OpenAsync();

Console.WriteLine("Proposta.Migrator: aguardando lock de coordenação (pg_advisory_lock)...");
await using (var lockCommand = lockConnection.CreateCommand())
{
    lockCommand.CommandText = "SELECT pg_advisory_lock(@lockId)";
    lockCommand.Parameters.AddWithValue("lockId", AdvisoryLockId);
    await lockCommand.ExecuteNonQueryAsync();
}

try
{
    var optionsBuilder = new DbContextOptionsBuilder<PropostaDbContext>()
        .UseNpgsql(connectionString, npgsql => npgsql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), errorCodesToAdd: null));

    await using var dbContext = new PropostaDbContext(optionsBuilder.Options);

    var pending = (await dbContext.Database.GetPendingMigrationsAsync()).ToList();
    if (pending.Count == 0)
    {
        Console.WriteLine("Proposta.Migrator: nenhuma migration pendente.");
        return 0;
    }

    Console.WriteLine($"Proposta.Migrator: aplicando {pending.Count} migration(ns): {string.Join(", ", pending)}");
    await dbContext.Database.MigrateAsync();
    Console.WriteLine("Proposta.Migrator: migrations aplicadas com sucesso.");
    return 0;
}
catch (Exception ex)
{
    await Console.Error.WriteLineAsync($"Proposta.Migrator: falha ao aplicar migrations: {ex}");
    return 1;
}
finally
{
    await using var unlockCommand = lockConnection.CreateCommand();
    unlockCommand.CommandText = "SELECT pg_advisory_unlock(@lockId)";
    unlockCommand.Parameters.AddWithValue("lockId", AdvisoryLockId);
    await unlockCommand.ExecuteNonQueryAsync();
}
