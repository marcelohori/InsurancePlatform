using BuildingBlocks.Contracts.Events;
using Contratacao.Application.Dtos;
using Contratacao.Application.Exceptions;
using Contratacao.Application.UseCases;
using Xunit;

namespace Contratacao.UnitTests.UseCases;

public class CriarContratacaoUseCaseTests
{
    private static CriarContratacaoCommand ComandoValido(Guid propostaId, string? idempotencyKey = null) => new(
        propostaId,
        new DateOnly(2026, 1, 1),
        null,
        null,
        1200m,
        null,
        idempotencyKey);

    private static (CriarContratacaoUseCase UseCase, FakeContratacaoRepository Repositorio, FakePropostaVerificationPort Verificador, FakeIdempotencyStore Idempotencia, FakeEventPublisher Publicador)
        CriarUseCase()
    {
        var repositorio = new FakeContratacaoRepository();
        var verificador = new FakePropostaVerificationPort();
        var idempotencia = new FakeIdempotencyStore();
        var publicador = new FakeEventPublisher();
        var useCase = new CriarContratacaoUseCase(repositorio, verificador, idempotencia, publicador, new FakeUnitOfWork());
        return (useCase, repositorio, verificador, idempotencia, publicador);
    }

    [Fact]
    public async Task Executar_Com_Proposta_Aprovada_Cria_Contratacao_E_Publica_Evento()
    {
        var (useCase, _, verificador, _, publicador) = CriarUseCase();
        var propostaId = Guid.NewGuid();
        verificador.DefinirStatus(propostaId, "Aprovada");

        var dto = await useCase.ExecutarAsync(ComandoValido(propostaId), CancellationToken.None);

        Assert.Equal(propostaId, dto.PropostaId);
        var evento = Assert.Single(publicador.EventosPublicados);
        Assert.IsType<ContratacaoEfetuadaEvent>(evento);
    }

    [Fact]
    public async Task Executar_Com_Proposta_Inexistente_Lanca_PropostaNaoEncontradaException()
    {
        var (useCase, _, _, _, _) = CriarUseCase();

        await Assert.ThrowsAsync<PropostaNaoEncontradaException>(
            () => useCase.ExecutarAsync(ComandoValido(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Executar_Com_Proposta_Nao_Aprovada_Lanca_PropostaNaoAprovadaException()
    {
        var (useCase, _, verificador, _, _) = CriarUseCase();
        var propostaId = Guid.NewGuid();
        verificador.DefinirStatus(propostaId, "EmAnalise");

        await Assert.ThrowsAsync<PropostaNaoAprovadaException>(
            () => useCase.ExecutarAsync(ComandoValido(propostaId), CancellationToken.None));
    }

    [Fact]
    public async Task Executar_Repetido_Com_Mesma_Chave_De_Idempotencia_Nao_Cria_Segunda_Contratacao()
    {
        var (useCase, repositorio, verificador, _, publicador) = CriarUseCase();
        var propostaId = Guid.NewGuid();
        verificador.DefinirStatus(propostaId, "Aprovada");
        var comando = ComandoValido(propostaId, idempotencyKey: "chave-123");

        var primeira = await useCase.ExecutarAsync(comando, CancellationToken.None);
        var segunda = await useCase.ExecutarAsync(comando, CancellationToken.None);

        Assert.Equal(primeira.Id, segunda.Id);
        Assert.Single(publicador.EventosPublicados);
        Assert.Single(await repositorio.ListarAsync(1, 10, CancellationToken.None));
    }

    [Fact]
    public async Task Executar_Com_Mesma_Chave_E_Outro_Payload_Lanca_Conflito()
    {
        var (useCase, _, verificador, _, _) = CriarUseCase();
        var propostaId = Guid.NewGuid();
        verificador.DefinirStatus(propostaId, "Aprovada");
        var comando = ComandoValido(propostaId, idempotencyKey: "chave-123");

        await useCase.ExecutarAsync(comando, CancellationToken.None);

        await Assert.ThrowsAsync<Contratacao.Domain.Exceptions.ConflictDomainException>(
            () => useCase.ExecutarAsync(comando with { ValorPremio = 999m }, CancellationToken.None));
    }
}


