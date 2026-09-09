using Proposta.Application.UseCases;
using Proposta.Domain;
using Proposta.Domain.Exceptions;
using Proposta.Domain.ValueObjects;
using Xunit;

namespace Proposta.UnitTests.UseCases;

public class ListarEObterPropostaUseCaseTests
{
    private static PropostaSeguro NovaProposta() => PropostaSeguro.Criar(
        "Maria Silva",
        DocumentoIdentificacao.Criar("11144477735"),
        TipoSeguro.Auto,
        Monetario.Criar(50000m),
        Monetario.Criar(1200m));

    [Fact]
    public async Task Listar_Sem_Propostas_Retorna_Lista_Vazia()
    {
        var repositorio = new FakePropostaRepository();
        var useCase = new ListarPropostasUseCase(repositorio);

        var resultado = await useCase.ExecutarAsync(1, 10, CancellationToken.None);

        Assert.Empty(resultado);
    }

    [Fact]
    public async Task Listar_Com_Propostas_Retorna_Todas_Na_Pagina()
    {
        var repositorio = new FakePropostaRepository();
        repositorio.Adicionar(NovaProposta());
        repositorio.Adicionar(NovaProposta());
        var useCase = new ListarPropostasUseCase(repositorio);

        var resultado = await useCase.ExecutarAsync(1, 10, CancellationToken.None);

        Assert.Equal(2, resultado.Count);
    }

    [Fact]
    public async Task ObterPorId_Existente_Retorna_Dto()
    {
        var repositorio = new FakePropostaRepository();
        var proposta = NovaProposta();
        repositorio.Adicionar(proposta);
        var useCase = new ObterPropostaPorIdUseCase(repositorio);

        var dto = await useCase.ExecutarAsync(proposta.Id, CancellationToken.None);

        Assert.NotNull(dto);
        Assert.Equal(proposta.Id, dto!.Id);
    }

    [Fact]
    public async Task ObterPorId_Inexistente_Retorna_Null()
    {
        var repositorio = new FakePropostaRepository();
        var useCase = new ObterPropostaPorIdUseCase(repositorio);

        var dto = await useCase.ExecutarAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Null(dto);
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public async Task Listar_Com_Paginacao_Invalida_Lanca_DomainException(int pagina, int tamanhoPagina)
    {
        var useCase = new ListarPropostasUseCase(new FakePropostaRepository());

        await Assert.ThrowsAsync<DomainException>(() =>
            useCase.ExecutarAsync(pagina, tamanhoPagina, CancellationToken.None));
    }
}


