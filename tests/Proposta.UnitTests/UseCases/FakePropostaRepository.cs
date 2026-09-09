using Proposta.Application.Ports;
using Proposta.Domain;

namespace Proposta.UnitTests.UseCases;

public sealed class FakePropostaRepository : IPropostaRepository
{
    private readonly Dictionary<Guid, PropostaSeguro> _propostas = [];

    public void Adicionar(PropostaSeguro proposta) => _propostas[proposta.Id] = proposta;

    public Task<PropostaSeguro?> ObterPorIdAsync(Guid id, string? criadoPor, CancellationToken cancellationToken) =>
        Task.FromResult(_propostas.Values.FirstOrDefault(p => p.Id == id && (criadoPor == null || p.CriadoPor == criadoPor)));

    public Task<PropostaSeguro?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken) =>
        ObterPorIdAsync(id, null, cancellationToken);

    public Task<IReadOnlyList<PropostaSeguro>> ListarAsync(int pagina, int tamanhoPagina, string? criadoPor, CancellationToken cancellationToken)
    {
        IReadOnlyList<PropostaSeguro> resultado = [.. _propostas.Values
            .Where(p => criadoPor == null || p.CriadoPor == criadoPor)
            .Skip((pagina - 1) * tamanhoPagina)
            .Take(tamanhoPagina)];
        return Task.FromResult(resultado);
    }

    public Task<IReadOnlyList<PropostaSeguro>> ListarAsync(int pagina, int tamanhoPagina, CancellationToken cancellationToken) =>
        ListarAsync(pagina, tamanhoPagina, null, cancellationToken);

    public void Atualizar(PropostaSeguro proposta) => _propostas[proposta.Id] = proposta;

    public void Remover(PropostaSeguro proposta) => _propostas.Remove(proposta.Id);
}


