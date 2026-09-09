using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Contratacao.Application.Contracts;
using Contratacao.Application.Dtos;
using Xunit;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace Contratacao.IntegrationTests;

[Collection(nameof(ContratacaoApiCollection))]
public sealed class IdempotencyConcurrencyTests(ContratacaoApiFactory factory) : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly HttpClient _client = factory.CreateClient();
    private readonly ContratacaoApiFactory _factory = factory;

    public void Dispose()
    {
        _factory.PropostaApiStub.Reset();
        GC.SuppressFinalize(this);
    }

    private void SetupAuth()
    {
        var token = ContratacaoApiFactory.GerarTokenAcesso("analyst-1", "analista");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private void StubProposta(Guid propostaId, string status) =>
        _factory.PropostaApiStub
            .Given(Request.Create().WithPath($"/api/v1/propostas/{propostaId}").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(new { id = propostaId, status }));

    private static object CriarPayload(Guid propostaId)
    {
        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
        return new
        {
            propostaId,
            dataContratacao = hoje.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            dataInicioVigencia = hoje.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            dataFimVigencia = hoje.AddYears(1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            valorPremio = 1200m,
            moedaValorPremio = "BRL",
        };
    }

    [Fact]
    public async Task DuasRequisicoesConcorrentesMesmaIdempotencyKey_AmbasRetornam201_ComMesmoId()
    {
        SetupAuth();
        var propostaId = Guid.NewGuid();
        StubProposta(propostaId, "Aprovada");

        var idempotencyKey = Guid.NewGuid().ToString();
        var request = CriarPayload(propostaId);

        var task1 = SendCreateRequest(request, idempotencyKey);
        var task2 = SendCreateRequest(request, idempotencyKey);

        var (response1, dto1) = await task1;
        var (response2, dto2) = await task2;

        Assert.Equal(HttpStatusCode.Created, response1.StatusCode);
        Assert.Equal(HttpStatusCode.Created, response2.StatusCode);
        Assert.Equal(dto1.Id, dto2.Id);
    }

    [Fact]
    public async Task DuasRequisicoesMesmaIdempotencyKey_MasTempoDecorrido_RetornamDiferentesIds()
    {
        SetupAuth();
        var propostaId = Guid.NewGuid();
        StubProposta(propostaId, "Aprovada");

        var idempotencyKey = Guid.NewGuid().ToString();
        var request = CriarPayload(propostaId);

        var (response1, dto1) = await SendCreateRequest(request, idempotencyKey);
        await Task.Delay(100);
        var (response2, dto2) = await SendCreateRequest(request, idempotencyKey);

        Assert.Equal(HttpStatusCode.Created, response1.StatusCode);
        Assert.Equal(HttpStatusCode.Created, response2.StatusCode);
    }

    [Fact]
    public async Task IdempotencyKeyInvalidFormat_Retorna400()
    {
        SetupAuth();
        var propostaId = Guid.NewGuid();
        StubProposta(propostaId, "Aprovada");

        var request = CriarPayload(propostaId);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1.0/contratacoes");
        httpRequest.Headers.Add("Idempotency-Key", "invalid-key-not-uuid");
        httpRequest.Content = JsonContent.Create(request);

        var response = await _client.SendAsync(httpRequest);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<(HttpResponseMessage, ContratacaoDto)> SendCreateRequest(object request, string idempotencyKey)
    {
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1.0/contratacoes");
        httpRequest.Headers.Add("Idempotency-Key", idempotencyKey);
        httpRequest.Content = JsonContent.Create(request);

        var response = await _client.SendAsync(httpRequest);
        var dto = await ReadAsAsync<ContratacaoDto>(response.Content);

        return (response, dto!);
    }

    private static async Task<T?> ReadAsAsync<T>(HttpContent content)
    {
        var json = await content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(json);
    }
}
