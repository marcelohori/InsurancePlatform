namespace Contratacao.Application.Ports;

/// <summary>
/// Tracks idempotency keys for contratação creation, so a retried request returns the
/// original result instead of creating a duplicate apólice.
/// </summary>
public interface IIdempotencyStore
{
    Task<IdempotencyEntry?> ObterAsync(string chaveIdempotencia, CancellationToken cancellationToken);

    void Registrar(string chaveIdempotencia, Guid contratacaoId, string hashRequisicao);
}

public sealed record IdempotencyEntry(Guid ContratacaoId, string HashRequisicao);
