using Contratacao.Application.Ports;

namespace Contratacao.UnitTests.UseCases;

public sealed class FakePropostaVerificationPort : IPropostaVerificationPort
{
    private readonly Dictionary<Guid, string> _statusPorProposta = [];

    public void DefinirStatus(Guid propostaId, string status) => _statusPorProposta[propostaId] = status;

    public Task<PropostaVerificationResult?> VerificarAsync(Guid propostaId, CancellationToken cancellationToken)
    {
        if (!_statusPorProposta.TryGetValue(propostaId, out var status))
        {
            return Task.FromResult<PropostaVerificationResult?>(null);
        }

        return Task.FromResult<PropostaVerificationResult?>(new PropostaVerificationResult(propostaId, status));
    }
}


