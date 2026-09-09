using Contratacao.Application.UseCases;
using Contratacao.Domain.Exceptions;
using Xunit;

namespace Contratacao.UnitTests.UseCases;

public class ListarContratacoesUseCaseTests
{
    [Theory]
    [InlineData(0, 10)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public async Task Listar_Com_Paginacao_Invalida_Lanca_DomainException(int pagina, int tamanhoPagina)
    {
        var useCase = new ListarContratacoesUseCase(new FakeContratacaoRepository());

        await Assert.ThrowsAsync<DomainException>(() =>
            useCase.ExecutarAsync(pagina, tamanhoPagina, CancellationToken.None));
    }
}
