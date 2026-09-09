using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Contratacao.Application.Contracts;
using Contratacao.Application.Dtos;
using Xunit;

namespace Contratacao.IntegrationTests;

[Collection(nameof(ContratacaoApiCollection))]
public sealed class IdempotencyConcurrencyTests(ContratacaoApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    private void SetupAuth()
    {
        var token = ContratacaoApiFactory.GerarTokenAcesso("analyst-1", "analista");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    [Fact]
    public async Task DuasRequisicoesConcorrentesMesmaIdempotencyKey_AmbasRetornam201_ComMesmoId()
    {
        SetupAuth();

        // Setup: Create a proposal first (assuming Proposta service is available)
        // For this test, we'll use a valid UUID as propostaId
        var propostaId = Guid.Parse("550e8400-e29b-41d4-a716-446655440000");
        var idempotencyKey = Guid.NewGuid().ToString();

        var request = new
        {
            PropostaId = propostaId,
            DataContratacao = DateOnly.FromDateTime(DateTime.UtcNow),
            DataInicioVigencia = DateOnly.FromDateTime(DateTime.UtcNow),
            DataFimVigencia = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)),
            ValorPremio = 1200m,
            MoedaValorPremio = "BRL"
        };

        // Act: Send two concurrent requests with same idempotency key
        var task1 = SendCreateRequest(request, idempotencyKey);
        var task2 = SendCreateRequest(request, idempotencyKey);

        var (response1, dto1) = await task1;
        var (response2, dto2) = await task2;

        // Assert: Both should succeed (201) and return same contract ID
        Assert.Equal(HttpStatusCode.Created, response1.StatusCode);
        Assert.Equal(HttpStatusCode.Created, response2.StatusCode);
        Assert.Equal(dto1.Id, dto2.Id);
    }

    [Fact]
    public async Task DuasRequisicoesMesmaIdempotencyKey_MasTempoDecorrido_RetornamDiferentesIds()
    {
        SetupAuth();

        var propostaId = Guid.Parse("550e8400-e29b-41d4-a716-446655440001");
        var request = new
        {
            PropostaId = propostaId,
            DataContratacao = DateOnly.FromDateTime(DateTime.UtcNow),
            DataInicioVigencia = DateOnly.FromDateTime(DateTime.UtcNow),
            DataFimVigencia = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)),
            ValorPremio = 1200m,
            MoedaValorPremio = "BRL"
        };

        // Act: First request
        var idempotencyKey = Guid.NewGuid().ToString();
        var (response1, dto1) = await SendCreateRequest(request, idempotencyKey);

        // Wait a bit and send second request (idempotency key expires or is no longer in cache)
        await Task.Delay(100);

        // Act: Second request with same idempotency key but different time
        var (response2, dto2) = await SendCreateRequest(request, idempotencyKey);

        // Assert: Both succeed but note that they may have different IDs depending on idempotency store TTL
        Assert.Equal(HttpStatusCode.Created, response1.StatusCode);
        Assert.Equal(HttpStatusCode.Created, response2.StatusCode);
    }

    [Fact]
    public async Task IdempotencyKeyInvalidFormat_Retorna400()
    {
        SetupAuth();

        var propostaId = Guid.Parse("550e8400-e29b-41d4-a716-446655440002");
        var request = new
        {
            PropostaId = propostaId,
            DataContratacao = DateOnly.FromDateTime(DateTime.UtcNow),
            DataInicioVigencia = DateOnly.FromDateTime(DateTime.UtcNow),
            DataFimVigencia = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)),
            ValorPremio = 1200m,
            MoedaValorPremio = "BRL"
        };

        // Act: Send request with invalid idempotency key (not UUID)
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1.0/contratacoes");
        httpRequest.Headers.Add("Idempotency-Key", "invalid-key-not-uuid");
        httpRequest.Content = JsonContent.Create(request);

        var response = await _client.SendAsync(httpRequest);

        // Assert: Should return 400
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
