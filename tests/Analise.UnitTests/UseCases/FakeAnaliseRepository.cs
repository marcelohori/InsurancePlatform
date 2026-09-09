using Analise.Application.Ports;
using Analise.Domain;

namespace Analise.UnitTests.UseCases;

public sealed class FakeAnaliseRepository : IAnaliseRepository
{
    private readonly Dictionary<Guid, AnaliseRisco> _porPropostaId = [];

    public void Adicionar(AnaliseRisco analise) => _porPropostaId[analise.PropostaId] = analise;

    public Task<AnaliseRisco?> ObterPorPropostaIdAsync(Guid propostaId, CancellationToken cancellationToken) =>
        Task.FromResult(_porPropostaId.GetValueOrDefault(propostaId));

    public void Atualizar(AnaliseRisco analise) => _porPropostaId[analise.PropostaId] = analise;
}

