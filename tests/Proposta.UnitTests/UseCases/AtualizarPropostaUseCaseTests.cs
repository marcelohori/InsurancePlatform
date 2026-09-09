using Proposta.Application.Dtos;
using Proposta.Application.Exceptions;
using Proposta.Application.UseCases;
using Proposta.Domain;
using Proposta.Domain.Exceptions;
using Proposta.Domain.ValueObjects;
using Xunit;

namespace Proposta.UnitTests.UseCases;

public class AtualizarPropostaUseCaseTests
{
    private static PropostaSeguro NovaProposta() => PropostaSeguro.Criar(
        "Maria Silva",
        DocumentoIdentificacao.Criar("11144477735"),
        TipoSeguro.Auto,
        Monetario.Criar(50000m),
        Monetario.Criar(1200m));

    private static AtualizarPropostaCommand ComandoDe(PropostaSeguro proposta, StatusProposta status) => new(
        proposta.Id,
        proposta.NomeSegurado,
        proposta.DocumentoSegurado.Numero,
        proposta.TipoSeguro.ToString(),
        proposta.ValorCobertura.Quantia,
        proposta.ValorCobertura.Moeda,
        proposta.ValorPremio.Quantia,
        proposta.ValorPremio.Moeda,
        status);

    [Fact]
    public async Task Executar_Aprovando_Proposta_EmAnalise_Transiciona_Status()
    {
        var repositorio = new FakePropostaRepository();
        var proposta = NovaProposta();
        repositorio.Adicionar(proposta);
        var useCase = new AtualizarPropostaUseCase(repositorio, new FakeUnitOfWork());

        var dto = await useCase.ExecutarAsync(ComandoDe(proposta, StatusProposta.Aprovada), CancellationToken.None);

        Assert.Equal(StatusProposta.Aprovada, dto.Status);
    }

    [Fact]
    public async Task Executar_Alterando_Status_De_Proposta_Ja_Aprovada_Lanca_ConflictDomainException()
    {
        var repositorio = new FakePropostaRepository();
        var proposta = NovaProposta();
        proposta.Aprovar();
        repositorio.Adicionar(proposta);
        var useCase = new AtualizarPropostaUseCase(repositorio, new FakeUnitOfWork());

        await Assert.ThrowsAsync<ConflictDomainException>(
            () => useCase.ExecutarAsync(ComandoDe(proposta, StatusProposta.Rejeitada), CancellationToken.None));
    }

    [Fact]
    public async Task Executar_Com_Id_Inexistente_Lanca_PropostaNaoEncontradaException()
    {
        var repositorio = new FakePropostaRepository();
        var proposta = NovaProposta();
        var useCase = new AtualizarPropostaUseCase(repositorio, new FakeUnitOfWork());

        await Assert.ThrowsAsync<PropostaNaoEncontradaException>(
            () => useCase.ExecutarAsync(ComandoDe(proposta, StatusProposta.Aprovada), CancellationToken.None));
    }
}


