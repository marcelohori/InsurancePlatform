using Analise.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

// Fixed per-service id for pg_advisory_lock - advisory locks are scoped to the connected
// database, so this only needs to be unique within analise_db, not across services.
const long AdvisoryLockId = 721_003;

var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__AnaliseDbMigrator")
    ?? Environment.GetEnvironmentVariable("ConnectionStrings__AnaliseDb")
    ?? throw new InvalidOperationException(
        "Connection string não configurada. Defina 'ConnectionStrings__AnaliseDbMigrator' com um usuário que tenha permissão de DDL no schema.");

await using var lockConnection = new NpgsqlConnection(connectionString);
await lockConnection.OpenAsync();

Console.WriteLine("Analise.Migrator: aguardando lock de coordenação (pg_advisory_lock)...");
await using (var lockCommand = lockConnection.CreateCommand())
{
    lockCommand.CommandText = "SELECT pg_advisory_lock(@lockId)";
    lockCommand.Parameters.AddWithValue("lockId", AdvisoryLockId);
    await lockCommand.ExecuteNonQueryAsync();
}

try
{
    var optionsBuilder = new DbContextOptionsBuilder<AnaliseDbContext>()
        .UseNpgsql(connectionString, npgsql => npgsql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), errorCodesToAdd: null));

    await using var dbContext = new AnaliseDbContext(optionsBuilder.Options);

    var pending = (await dbContext.Database.GetPendingMigrationsAsync()).ToList();
    if (pending.Count == 0)
    {
        Console.WriteLine("Analise.Migrator: nenhuma migration pendente.");
        return 0;
    }

    Console.WriteLine($"Analise.Migrator: aplicando {pending.Count} migration(ns): {string.Join(", ", pending)}");
    await dbContext.Database.MigrateAsync();
    Console.WriteLine("Analise.Migrator: migrations aplicadas com sucesso.");
    return 0;
}
catch (Exception ex)
{
    await Console.Error.WriteLineAsync($"Analise.Migrator: falha ao aplicar migrations: {ex}");
    return 1;
}
finally
{
    await using var unlockCommand = lockConnection.CreateCommand();
    unlockCommand.CommandText = "SELECT pg_advisory_unlock(@lockId)";
    unlockCommand.Parameters.AddWithValue("lockId", AdvisoryLockId);
    await unlockCommand.ExecuteNonQueryAsync();
}
