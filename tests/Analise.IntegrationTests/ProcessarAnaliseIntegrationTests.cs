using Analise.Domain;
using Analise.Infrastructure.Persistence;
using BuildingBlocks.Contracts.Events;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WireMock.RequestBuilders;
using Xunit;
using WireMockResponse = WireMock.ResponseBuilders.Response;

namespace Analise.IntegrationTests;

[Collection(nameof(AnaliseProcessingCollection))]
public class ProcessarAnaliseIntegrationTests(AnaliseProcessingFixture fixture)
{
    [Fact]
    public async Task Consumir_PropostaCriadaEvent_Real_Processa_E_Persiste_Analise_Concluida()
    {
        fixture.AnthropicStub
            .Given(Request.Create().WithPath("/v1/messages").UsingPost())
            .RespondWith(WireMockResponse.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(new
                {
                    content = new[]
                    {
                        new { type = "text", text = "{\"score\": 65, \"recomendacao\": \"Aprovar\", \"justificativa\": \"Perfil dentro dos parÃ¢metros aceitÃ¡veis.\"}" },
                    },
                }));

        var propostaId = Guid.NewGuid();
        var evento = new PropostaCriadaEvent(
            propostaId,
            "Maria Silva",
            "11144477735",
            "Auto",
            50000m,
            "BRL",
            1200m,
            "BRL",
            DateTimeOffset.UtcNow);

        var publishEndpoint = fixture.Services.GetRequiredService<IPublishEndpoint>();
        await publishEndpoint.Publish(evento, CancellationToken.None);

        var analise = await AguardarAnaliseAsync(propostaId, TimeSpan.FromSeconds(20));

        Assert.NotNull(analise);
        Assert.Equal(StatusAnalise.Concluida, analise!.Status);
        Assert.Equal(65, analise.ScoreRisco);
        Assert.Equal(Recomendacao.Aprovar, analise.Recomendacao);
    }

    private async Task<AnaliseRisco?> AguardarAnaliseAsync(Guid propostaId, TimeSpan timeout)
    {
        var prazo = DateTime.UtcNow.Add(timeout);
        while (DateTime.UtcNow < prazo)
        {
            using var scope = fixture.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AnaliseDbContext>();
            var analise = await dbContext.Analises
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.PropostaId == propostaId && a.Status != StatusAnalise.EmProcessamento);

            if (analise is not null)
            {
                return analise;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(300));
        }

        return null;
    }
}

