using Contratacao.Application.Ports;

namespace Contratacao.UnitTests.UseCases;

public sealed class FakeIdempotencyStore : IIdempotencyStore
{
    private readonly Dictionary<string, IdempotencyEntry> _porChave = [];

    public Task<IdempotencyEntry?> ObterAsync(string chaveIdempotencia, CancellationToken cancellationToken) =>
        Task.FromResult<IdempotencyEntry?>(_porChave.TryGetValue(chaveIdempotencia, out var entry) ? entry : null);

    public void Registrar(string chaveIdempotencia, Guid contratacaoId, string hashRequisicao) =>
        _porChave[chaveIdempotencia] = new IdempotencyEntry(contratacaoId, hashRequisicao);
}


