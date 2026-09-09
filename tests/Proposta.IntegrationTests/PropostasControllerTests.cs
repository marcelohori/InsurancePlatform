using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using BuildingBlocks.Contracts.Events;
using MassTransit;
using Proposta.Application.Dtos;
using Xunit;

namespace Proposta.IntegrationTests;

[Collection(nameof(PropostaApiCollection))]
public class PropostasControllerTests : IAsyncLifetime
{
    // HttpClient's *AsJsonAsync helpers use their own default JsonSerializerOptions, separate
    // from the Api's AddJsonOptions - the client needs the same enum converter to round-trip
    // StatusProposta/TipoSeguro as strings.
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly PropostaApiFactory _factory;
    private readonly HttpClient _client;
    private readonly ConcurrentQueue<PropostaCriadaEvent> _eventosRecebidos = new();
    private IBusControl? _busConsumidorDeTeste;

    public PropostasControllerTests(PropostaApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", JwtTestTokenFactory.CriarToken());
    }

    public async Task InitializeAsync()
    {
        // Independent bus connected to the same broker, subscribing to the same message type
        // the Api publishes - proves the event actually reaches RabbitMQ, not just the outbox table.
        _busConsumidorDeTeste = Bus.Factory.CreateUsingRabbitMq(cfg =>
        {
            cfg.Host(new Uri($"rabbitmq://{_factory.RabbitMqHost}:{_factory.RabbitMqPort}/"), h =>
            {
                h.Username("test");
                h.Password("test");
            });

            cfg.ReceiveEndpoint("test-proposta-criada-consumer", e =>
            {
                e.Handler<PropostaCriadaEvent>(context =>
                {
                    _eventosRecebidos.Enqueue(context.Message);
                    return Task.CompletedTask;
                });
            });
        });

        await _busConsumidorDeTeste.StartAsync();
    }

    public async Task DisposeAsync()
    {
        if (_busConsumidorDeTeste is not null)
        {
            await _busConsumidorDeTeste.StopAsync();
        }
    }

    private static object CriarPayloadValido() => new
    {
        nomeSegurado = "Maria Silva",
        documentoSegurado = "11144477735",
        tipoSeguro = "Auto",
        valorCobertura = 50000m,
        moedaCobertura = (string?)null,
        valorPremio = 1200m,
        moedaPremio = (string?)null,
    };

    [Fact]
    public async Task Fluxo_Completo_Criar_Listar_ObterPorId_Atualizar_Deletar()
    {
        var criarResponse = await _client.PostAsJsonAsync("/api/v1/propostas", CriarPayloadValido());
        Assert.Equal(HttpStatusCode.Created, criarResponse.StatusCode);
        var criada = await criarResponse.Content.ReadFromJsonAsync<PropostaDto>(JsonOptions);
        Assert.NotNull(criada);

        var listarResponse = await _client.GetAsync("/api/v1/propostas");
        Assert.Equal(HttpStatusCode.OK, listarResponse.StatusCode);
        var lista = await listarResponse.Content.ReadFromJsonAsync<List<PropostaDto>>(JsonOptions);
        Assert.Contains(lista!, p => p.Id == criada!.Id);

        var obterResponse = await _client.GetAsync($"/api/v1/propostas/{criada!.Id}");
        Assert.Equal(HttpStatusCode.OK, obterResponse.StatusCode);

        var atualizarPayload = new
        {
            nomeSegurado = criada.NomeSegurado,
            documentoSegurado = criada.DocumentoSegurado,
            tipoSeguro = criada.TipoSeguro.ToString(),
            valorCobertura = criada.ValorCobertura,
            moedaCobertura = criada.MoedaCobertura,
            valorPremio = criada.ValorPremio,
            moedaPremio = criada.MoedaPremio,
            status = "Aprovada",
        };
        var atualizarResponse = await _client.PutAsJsonAsync($"/api/v1/propostas/{criada.Id}", atualizarPayload);
        Assert.Equal(HttpStatusCode.OK, atualizarResponse.StatusCode);
        var atualizada = await atualizarResponse.Content.ReadFromJsonAsync<PropostaDto>(JsonOptions);
        Assert.Equal(Domain.StatusProposta.Aprovada, atualizada!.Status);

        var deletarBloqueadoResponse = await _client.DeleteAsync($"/api/v1/propostas/{criada.Id}");
        Assert.Equal(HttpStatusCode.Conflict, deletarBloqueadoResponse.StatusCode);
    }

    [Fact]
    public async Task Criar_Proposta_Publica_PropostaCriadaEvent_No_Broker_Real()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/propostas", CriarPayloadValido());
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var criada = await response.Content.ReadFromJsonAsync<PropostaDto>(JsonOptions);

        var recebido = await AguardarEventoAsync(criada!.Id, TimeSpan.FromSeconds(15));

        Assert.True(recebido, "PropostaCriadaEvent nÃ£o foi recebido do broker real dentro do tempo esperado.");
    }

    [Fact]
    public async Task Deletar_Proposta_EmAnalise_Retorna_204_E_Some_Da_Listagem()
    {
        var criarResponse = await _client.PostAsJsonAsync("/api/v1/propostas", CriarPayloadValido());
        var criada = await criarResponse.Content.ReadFromJsonAsync<PropostaDto>(JsonOptions);

        var deletarResponse = await _client.DeleteAsync($"/api/v1/propostas/{criada!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deletarResponse.StatusCode);

        var obterResponse = await _client.GetAsync($"/api/v1/propostas/{criada.Id}");
        Assert.Equal(HttpStatusCode.NotFound, obterResponse.StatusCode);
    }

    [Fact]
    public async Task Requisicao_Sem_Token_Retorna_401()
    {
        using var clienteSemToken = _factory.CreateClient();

        var response = await clienteSemToken.GetAsync("/api/v1/propostas");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Atualizar_Sem_Papel_Analista_Retorna_403()
    {
        var criarResponse = await _client.PostAsJsonAsync("/api/v1/propostas", CriarPayloadValido());
        var criada = await criarResponse.Content.ReadFromJsonAsync<PropostaDto>(JsonOptions);

        using var clienteCliente = _factory.CreateClient();
        clienteCliente.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", JwtTestTokenFactory.CriarToken(papel: "Cliente"));

        var atualizarPayload = new
        {
            nomeSegurado = criada!.NomeSegurado,
            documentoSegurado = criada.DocumentoSegurado,
            tipoSeguro = criada.TipoSeguro.ToString(),
            valorCobertura = criada.ValorCobertura,
            moedaCobertura = criada.MoedaCobertura,
            valorPremio = criada.ValorPremio,
            moedaPremio = criada.MoedaPremio,
            status = "Aprovada",
        };

        var response = await clienteCliente.PutAsJsonAsync($"/api/v1/propostas/{criada.Id}", atualizarPayload);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Usuario_Lista_Apenas_Propostas_Do_Proprio_Usuario()
    {
        using var clienteA = _factory.CreateClient();
        clienteA.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", JwtTestTokenFactory.CriarToken("usuario", "usuario-a"));

        using var clienteB = _factory.CreateClient();
        clienteB.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", JwtTestTokenFactory.CriarToken("usuario", "usuario-b"));

        var respostaA = await clienteA.PostAsJsonAsync("/api/v1/propostas", CriarPayloadValido());
        var propostaA = await respostaA.Content.ReadFromJsonAsync<PropostaDto>(JsonOptions);
        var respostaB = await clienteB.PostAsJsonAsync("/api/v1/propostas", CriarPayloadValido());
        var propostaB = await respostaB.Content.ReadFromJsonAsync<PropostaDto>(JsonOptions);

        Assert.Equal(HttpStatusCode.Created, respostaA.StatusCode);
        Assert.Equal(HttpStatusCode.Created, respostaB.StatusCode);

        var listaA = await clienteA.GetFromJsonAsync<List<PropostaDto>>("/api/v1/propostas", JsonOptions);
        var listaB = await clienteB.GetFromJsonAsync<List<PropostaDto>>("/api/v1/propostas", JsonOptions);

        Assert.Contains(listaA!, proposta => proposta.Id == propostaA!.Id);
        Assert.DoesNotContain(listaA!, proposta => proposta.Id == propostaB!.Id);
        Assert.Contains(listaB!, proposta => proposta.Id == propostaB!.Id);
        Assert.DoesNotContain(listaB!, proposta => proposta.Id == propostaA!.Id);
    }

    private async Task<bool> AguardarEventoAsync(Guid propostaId, TimeSpan timeout)
    {
        var prazo = DateTime.UtcNow.Add(timeout);
        while (DateTime.UtcNow < prazo)
        {
            if (_eventosRecebidos.Any(e => e.PropostaId == propostaId))
            {
                return true;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(200));
        }

        return false;
    }
}


