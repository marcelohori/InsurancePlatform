namespace Contratacao.Infrastructure.Persistence;

/// <summary>Maps an idempotency key to the contratação it originally created.</summary>
public sealed class IdempotencyRecord
{
    public string Chave { get; init; } = string.Empty;

    public Guid ContratacaoId { get; init; }

    public string HashRequisicao { get; init; } = string.Empty;

    public DateTimeOffset CriadoEm { get; init; }
}
