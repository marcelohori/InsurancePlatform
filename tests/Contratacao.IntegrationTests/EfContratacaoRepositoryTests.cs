using Contratacao.Domain;
using Contratacao.Domain.ValueObjects;
using Contratacao.Infrastructure.Persistence;
using Xunit;

namespace Contratacao.IntegrationTests;

[Collection(nameof(PostgresCollection))]
public class EfContratacaoRepositoryTests(PostgresFixture fixture)
{
    private static ApoliceSeguro NovaApolice() => Domain.ApoliceSeguro.Criar(
        Guid.NewGuid(),
        new DateOnly(2026, 1, 1),
        Monetario.Criar(1200m));

    [Fact]
    public async Task Adicionar_E_ObterPorId_Persiste_E_Recupera_A_Contratacao_Do_Postgres_Real()
    {
        var apolice = NovaApolice();

        await using (var dbContext = fixture.CreateDbContext())
        {
            var repositorio = new EfContratacaoRepository(dbContext);
            repositorio.Adicionar(apolice);
            await dbContext.SaveChangesAsync(CancellationToken.None);
        }

        await using var dbContextLeitura = fixture.CreateDbContext();
        var repositorioLeitura = new EfContratacaoRepository(dbContextLeitura);
        var recuperada = await repositorioLeitura.ObterPorIdAsync(apolice.Id, CancellationToken.None);

        Assert.NotNull(recuperada);
        Assert.Equal(apolice.NumeroApolice, recuperada!.NumeroApolice);
        Assert.Equal(apolice.Vigencia, recuperada.Vigencia);
        Assert.Equal(apolice.ValorPremio, recuperada.ValorPremio);
        Assert.Equal(StatusContratacao.Ativa, recuperada.Status);
    }
}

