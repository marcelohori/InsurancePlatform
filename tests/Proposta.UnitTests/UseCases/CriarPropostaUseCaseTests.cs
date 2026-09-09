using BuildingBlocks.Contracts.Events;
using Proposta.Application.Dtos;
using Proposta.Application.UseCases;
using Proposta.Domain;
using Proposta.Domain.Exceptions;
using Xunit;

namespace Proposta.UnitTests.UseCases;

public class CriarPropostaUseCaseTests
{
    private static CriarPropostaCommand ComandoValido() => new(
        "Maria Silva",
        "11144477735",
        nameof(TipoSeguro.Auto),
        50000m,
        null,
        1200m,
        null);

    [Fact]
    public async Task Executar_Com_Dados_Validos_Persiste_E_Publica_Evento()
    {
        var repositorio = new FakePropostaRepository();
        var publicador = new FakeEventPublisher();
        var useCase = new CriarPropostaUseCase(repositorio, publicador, new FakeUnitOfWork());

        var dto = await useCase.ExecutarAsync(ComandoValido(), CancellationToken.None);

        Assert.Equal(StatusProposta.EmAnalise, dto.Status);
        Assert.NotNull(await repositorio.ObterPorIdAsync(dto.Id, CancellationToken.None));
        var evento = Assert.Single(publicador.EventosPublicados);
        Assert.IsType<PropostaCriadaEvent>(evento);
        Assert.Equal(dto.Id, ((PropostaCriadaEvent)evento).PropostaId);
    }

    [Fact]
    public async Task Executar_Com_Documento_Invalido_Lanca_DomainException_E_Nao_Persiste()
    {
        var repositorio = new FakePropostaRepository();
        var publicador = new FakeEventPublisher();
        var useCase = new CriarPropostaUseCase(repositorio, publicador, new FakeUnitOfWork());
        var comando = ComandoValido() with { DocumentoSegurado = "123" };

        await Assert.ThrowsAsync<DomainException>(() => useCase.ExecutarAsync(comando, CancellationToken.None));

        Assert.Empty(publicador.EventosPublicados);
    }

    [Fact]
    public async Task Executar_Com_Valor_Negativo_Lanca_DomainException()
    {
        var repositorio = new FakePropostaRepository();
        var publicador = new FakeEventPublisher();
        var useCase = new CriarPropostaUseCase(repositorio, publicador, new FakeUnitOfWork());
        var comando = ComandoValido() with { ValorCobertura = -1m };

        await Assert.ThrowsAsync<DomainException>(() => useCase.ExecutarAsync(comando, CancellationToken.None));
    }

    [Fact]
    public async Task Executar_Com_Tipo_De_Seguro_Invalido_Lanca_DomainException()
    {
        var repositorio = new FakePropostaRepository();
        var publicador = new FakeEventPublisher();
        var useCase = new CriarPropostaUseCase(repositorio, publicador, new FakeUnitOfWork());
        var comando = ComandoValido() with { TipoSeguro = "Inexistente" };

        await Assert.ThrowsAsync<DomainException>(() => useCase.ExecutarAsync(comando, CancellationToken.None));
        Assert.Empty(publicador.EventosPublicados);
    }
}


