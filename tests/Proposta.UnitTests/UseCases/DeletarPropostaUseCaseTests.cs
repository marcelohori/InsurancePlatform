using Proposta.Application.Exceptions;
using Proposta.Application.UseCases;
using Proposta.Domain;
using Proposta.Domain.Exceptions;
using Proposta.Domain.ValueObjects;
using Xunit;

namespace Proposta.UnitTests.UseCases;

public class DeletarPropostaUseCaseTests
{
    private static PropostaSeguro NovaProposta() => PropostaSeguro.Criar(
        "Maria Silva",
        DocumentoIdentificacao.Criar("11144477735"),
        TipoSeguro.Auto,
        Monetario.Criar(50000m),
        Monetario.Criar(1200m));

    [Fact]
    public async Task Executar_Proposta_EmAnalise_Remove_Do_Repositorio()
    {
        var repositorio = new FakePropostaRepository();
        var proposta = NovaProposta();
        repositorio.Adicionar(proposta);
        var useCase = new DeletarPropostaUseCase(repositorio, new FakeUnitOfWork());

        await useCase.ExecutarAsync(proposta.Id, null, CancellationToken.None);

        Assert.Null(await repositorio.ObterPorIdAsync(proposta.Id, CancellationToken.None));
    }

    [Fact]
    public async Task Executar_Proposta_Aprovada_Lanca_ConflictDomainException()
    {
        var repositorio = new FakePropostaRepository();
        var proposta = NovaProposta();
        proposta.Aprovar();
        repositorio.Adicionar(proposta);
        var useCase = new DeletarPropostaUseCase(repositorio, new FakeUnitOfWork());

        await Assert.ThrowsAsync<ConflictDomainException>(() => useCase.ExecutarAsync(proposta.Id, null, CancellationToken.None));
        Assert.NotNull(await repositorio.ObterPorIdAsync(proposta.Id, CancellationToken.None));
    }

    [Fact]
    public async Task Executar_Com_Id_Inexistente_Lanca_PropostaNaoEncontradaException()
    {
        var repositorio = new FakePropostaRepository();
        var useCase = new DeletarPropostaUseCase(repositorio, new FakeUnitOfWork());

        await Assert.ThrowsAsync<PropostaNaoEncontradaException>(
            () => useCase.ExecutarAsync(Guid.NewGuid(), null, CancellationToken.None));
    }
}


