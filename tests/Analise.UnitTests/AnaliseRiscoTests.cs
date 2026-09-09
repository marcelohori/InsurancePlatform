using Analise.Domain;
using Analise.Domain.Exceptions;
using Xunit;

namespace Analise.UnitTests;

public class AnaliseRiscoTests
{
    [Fact]
    public void Iniciar_Retorna_Analise_Com_Status_EmProcessamento()
    {
        var analise = AnaliseRisco.Iniciar(Guid.NewGuid());

        Assert.Equal(StatusAnalise.EmProcessamento, analise.Status);
        Assert.Null(analise.ScoreRisco);
        Assert.Null(analise.Recomendacao);
    }

    [Fact]
    public void ConcluirComSucesso_Define_Score_Recomendacao_E_Justificativa()
    {
        var analise = AnaliseRisco.Iniciar(Guid.NewGuid());

        analise.ConcluirComSucesso(72, Domain.Recomendacao.Aprovar, "Perfil de risco moderado.");

        Assert.Equal(StatusAnalise.Concluida, analise.Status);
        Assert.Equal(72, analise.ScoreRisco);
        Assert.Equal(Domain.Recomendacao.Aprovar, analise.Recomendacao);
        Assert.NotNull(analise.DataConclusao);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void ConcluirComSucesso_Com_Score_Fora_Do_Intervalo_Lanca_DomainException(int scoreInvalido)
    {
        var analise = AnaliseRisco.Iniciar(Guid.NewGuid());

        Assert.Throws<DomainException>(() => analise.ConcluirComSucesso(scoreInvalido, Domain.Recomendacao.Aprovar, "x"));
    }

    [Fact]
    public void ConcluirComFalha_Define_Status_Falha()
    {
        var analise = AnaliseRisco.Iniciar(Guid.NewGuid());

        analise.ConcluirComFalha("Provedor de IA indisponÃ­vel.");

        Assert.Equal(StatusAnalise.Falha, analise.Status);
        Assert.Null(analise.ScoreRisco);
    }

    [Fact]
    public void ConcluirComSucesso_Quando_Ja_Concluida_Lanca_ConflictDomainException()
    {
        var analise = AnaliseRisco.Iniciar(Guid.NewGuid());
        analise.ConcluirComSucesso(50, Domain.Recomendacao.Aprovar, "x");

        Assert.Throws<ConflictDomainException>(() => analise.ConcluirComSucesso(50, Domain.Recomendacao.Aprovar, "x"));
    }

    [Fact]
    public void Reiniciar_A_Partir_De_Falha_Volta_Para_EmProcessamento()
    {
        var analise = AnaliseRisco.Iniciar(Guid.NewGuid());
        analise.ConcluirComFalha("timeout");

        analise.Reiniciar();

        Assert.Equal(StatusAnalise.EmProcessamento, analise.Status);
        Assert.Null(analise.Justificativa);
    }

    [Fact]
    public void Reiniciar_Quando_EmProcessamento_Lanca_ConflictDomainException()
    {
        var analise = AnaliseRisco.Iniciar(Guid.NewGuid());

        Assert.Throws<ConflictDomainException>(analise.Reiniciar);
    }
}

