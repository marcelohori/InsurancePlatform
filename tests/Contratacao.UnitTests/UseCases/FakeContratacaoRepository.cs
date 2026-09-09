using Contratacao.Application.Ports;
using Contratacao.Domain;

namespace Contratacao.UnitTests.UseCases;

public sealed class FakeContratacaoRepository : IContratacaoRepository
{
    private readonly Dictionary<Guid, ApoliceSeguro> _apolices = [];

    public void Adicionar(ApoliceSeguro apolice) => _apolices[apolice.Id] = apolice;

    public Task<ApoliceSeguro?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(_apolices.GetValueOrDefault(id));

    public Task<IReadOnlyList<ApoliceSeguro>> ListarAsync(int pagina, int tamanhoPagina, CancellationToken cancellationToken)
    {
        IReadOnlyList<ApoliceSeguro> resultado = [.. _apolices.Values.Skip((pagina - 1) * tamanhoPagina).Take(tamanhoPagina)];
        return Task.FromResult(resultado);
    }

    public void Atualizar(ApoliceSeguro apolice) => _apolices[apolice.Id] = apolice;

    public void Remover(ApoliceSeguro apolice) => _apolices.Remove(apolice.Id);
}


