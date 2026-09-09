using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Analise.Application.Dtos;
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

[Collection(nameof(AnaliseApiCollection))]
public class AnalisesControllerTests : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly AnaliseApiFactory _factory;
    private readonly HttpClient _client;

    public AnalisesControllerTests(AnaliseApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", JwtTestTokenFactory.CriarToken());
    }

    public void Dispose()
    {
        _factory.AnthropicStub.Reset();
        GC.SuppressFinalize(this);
    }

    private void StubAnthropicSucesso(int score, string recomendacao) =>
        _factory.AnthropicStub
            .Given(Request.Create().WithPath("/v1/messages").UsingPost())
            .RespondWith(WireMockResponse.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(new
                {
                    content = new[]
                    {
                        new { type = "text", text = $"{{\"score\": {score}, \"recomendacao\": \"{recomendacao}\", \"justificativa\": \"teste\"}}" },
                    },
                }));

    private void StubAnthropicFalha() =>
        _factory.AnthropicStub
            .Given(Request.Create().WithPath("/v1/messages").UsingPost())
            .RespondWith(WireMockResponse.Create().WithStatusCode(500));

    private static PropostaCriadaEvent NovoEvento(Guid propostaId) => new(
        propostaId, "Maria Silva", "11144477735", "Auto", 50000m, "BRL", 1200m, "BRL", DateTimeOffset.UtcNow);

    [Fact]
    public async Task Evento_Publicado_Duas_Vezes_Para_A_Mesma_Proposta_Gera_Apenas_Uma_Analise()
    {
        var propostaId = Guid.NewGuid();
        StubAnthropicSucesso(60, "Aprovar");

        using (var scope = _factory.Services.CreateScope())
        {
            var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();
            await publishEndpoint.Publish(NovoEvento(propostaId));
            await publishEndpoint.Publish(NovoEvento(propostaId));
        }

        var dto = await AguardarStatusDiferenteDeAsync(propostaId, StatusAnalise.EmProcessamento, TimeSpan.FromSeconds(20));
        Assert.NotNull(dto);
        Assert.Equal(StatusAnalise.Concluida, dto!.Status);

        using var verificacaoScope = _factory.Services.CreateScope();
        var dbContext = verificacaoScope.ServiceProvider.GetRequiredService<AnaliseDbContext>();
        var quantidade = await dbContext.Analises.CountAsync(a => a.PropostaId == propostaId);
        Assert.Equal(1, quantidade);
    }

    [Fact]
    public async Task Falha_Do_Provedor_De_IA_Marca_Analise_Como_Falha_E_Nao_Trava_Fila_Para_Proximo_Evento()
    {
        var propostaComFalhaId = Guid.NewGuid();
        StubAnthropicFalha();

        using (var scope = _factory.Services.CreateScope())
        {
            var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();
            await publishEndpoint.Publish(NovoEvento(propostaComFalhaId));
        }

        var dtoComFalha = await AguardarStatusDiferenteDeAsync(propostaComFalhaId, StatusAnalise.EmProcessamento, TimeSpan.FromSeconds(20));
        Assert.NotNull(dtoComFalha);
        Assert.Equal(StatusAnalise.Falha, dtoComFalha!.Status);

        // Queue must still be healthy for the next message after a failure.
        var propostaComSucessoId = Guid.NewGuid();
        _factory.AnthropicStub.Reset();
        StubAnthropicSucesso(80, "Aprovar");
        using (var scope = _factory.Services.CreateScope())
        {
            var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();
            await publishEndpoint.Publish(NovoEvento(propostaComSucessoId));
        }

        var dtoComSucesso = await AguardarStatusDiferenteDeAsync(propostaComSucessoId, StatusAnalise.EmProcessamento, TimeSpan.FromSeconds(20));
        Assert.NotNull(dtoComSucesso);
        Assert.Equal(StatusAnalise.Concluida, dtoComSucesso!.Status);
    }

    [Fact]
    public async Task ObterPorPropostaId_Sem_Analise_Registrada_Retorna_404()
    {
        var response = await _client.GetAsync($"/api/v1/propostas/{Guid.NewGuid()}/analise");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Requisicao_Sem_Token_Retorna_401()
    {
        using var clienteSemToken = _factory.CreateClient();

        var response = await clienteSemToken.GetAsync($"/api/v1/propostas/{Guid.NewGuid()}/analise");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task<AnaliseDto?> AguardarStatusDiferenteDeAsync(Guid propostaId, StatusAnalise statusIndesejado, TimeSpan timeout)
    {
        var prazo = DateTime.UtcNow.Add(timeout);
        while (DateTime.UtcNow < prazo)
        {
            var response = await _client.GetAsync($"/api/v1/propostas/{propostaId}/analise");
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var dto = await response.Content.ReadFromJsonAsync<AnaliseDto>(JsonOptions);
                if (dto is not null && dto.Status != statusIndesejado)
                {
                    return dto;
                }
            }

            await Task.Delay(TimeSpan.FromMilliseconds(300));
        }

        return null;
    }
}

