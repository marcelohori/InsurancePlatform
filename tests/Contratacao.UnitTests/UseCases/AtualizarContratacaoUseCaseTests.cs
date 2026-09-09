using Contratacao.Application.Dtos;
using Contratacao.Application.UseCases;
using Contratacao.Domain;
using Contratacao.Domain.Exceptions;
using Contratacao.Domain.ValueObjects;
using Xunit;

namespace Contratacao.UnitTests.UseCases;

public sealed class AtualizarContratacaoUseCaseTests
{
    [Fact]
    public async Task Executar_Com_Contratacao_Cancelada_Lanca_Conflito()
    {
        var repositorio = new FakeContratacaoRepository();
        var apolice = ApoliceSeguro.Criar(
            Guid.NewGuid(),
            new DateOnly(2026, 1, 1),
            Monetario.Criar(1200m));
        apolice.Cancelar();
        repositorio.Adicionar(apolice);

        var comando = new AtualizarContratacaoCommand(
            apolice.Id,
            new DateOnly(2026, 1, 1),
            new DateOnly(2027, 1, 1),
            1500m,
            "BRL",
            StatusContratacao.Cancelada);
        var useCase = new AtualizarContratacaoUseCase(repositorio, new FakeUnitOfWork());

        await Assert.ThrowsAsync<ConflictDomainException>(
            () => useCase.ExecutarAsync(comando, CancellationToken.None));
    }
}
