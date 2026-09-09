using Analise.Application.UseCases;
using Analise.Domain;
using Xunit;

namespace Analise.UnitTests.UseCases;

public class ConsultarAnalisePorPropostaUseCaseTests
{
    [Fact]
    public async Task Executar_Com_Analise_Existente_Retorna_Dto()
    {
        var repositorio = new FakeAnaliseRepository();
        var propostaId = Guid.NewGuid();
        var analise = AnaliseRisco.Iniciar(propostaId);
        repositorio.Adicionar(analise);
        var useCase = new ConsultarAnalisePorPropostaUseCase(repositorio);

        var dto = await useCase.ExecutarAsync(propostaId, CancellationToken.None);

        Assert.NotNull(dto);
        Assert.Equal(StatusAnalise.EmProcessamento, dto!.Status);
    }

    [Fact]
    public async Task Executar_Sem_Analise_Retorna_Null()
    {
        var repositorio = new FakeAnaliseRepository();
        var useCase = new ConsultarAnalisePorPropostaUseCase(repositorio);

        var dto = await useCase.ExecutarAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Null(dto);
    }
}

