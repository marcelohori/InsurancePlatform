using Contratacao.Application.Exceptions;
using Contratacao.Application.UseCases;
using Contratacao.Domain;
using Contratacao.Domain.ValueObjects;
using Xunit;

namespace Contratacao.UnitTests.UseCases;

public class DeletarContratacaoUseCaseTests
{
    [Fact]
    public async Task Executar_Remove_Apolice_Existente()
    {
        var repositorio = new FakeContratacaoRepository();
        var apolice = Domain.ApoliceSeguro.Criar(Guid.NewGuid(), new DateOnly(2026, 1, 1), Monetario.Criar(1200m));
        repositorio.Adicionar(apolice);
        var useCase = new DeletarContratacaoUseCase(repositorio, new FakeUnitOfWork());

        await useCase.ExecutarAsync(apolice.Id, CancellationToken.None);

        Assert.Null(await repositorio.ObterPorIdAsync(apolice.Id, CancellationToken.None));
    }

    [Fact]
    public async Task Executar_Com_Id_Inexistente_Lanca_ContratacaoNaoEncontradaException()
    {
        var repositorio = new FakeContratacaoRepository();
        var useCase = new DeletarContratacaoUseCase(repositorio, new FakeUnitOfWork());

        await Assert.ThrowsAsync<ContratacaoNaoEncontradaException>(
            () => useCase.ExecutarAsync(Guid.NewGuid(), CancellationToken.None));
    }
}


