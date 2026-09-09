using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Contratacao.Application.Dtos;
using Xunit;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace Contratacao.IntegrationTests;

[Collection(nameof(ContratacaoApiCollection))]
public class ContratacoesControllerTests : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly ContratacaoApiFactory _factory;
    private readonly HttpClient _client;

    public ContratacoesControllerTests(ContratacaoApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", JwtTestTokenFactory.CriarToken());
    }

    public void Dispose()
    {
        _factory.PropostaApiStub.Reset();
        GC.SuppressFinalize(this);
    }

    private void StubProposta(Guid propostaId, string status) =>
        _factory.PropostaApiStub
            .Given(Request.Create().WithPath($"/api/v1/propostas/{propostaId}").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(new { id = propostaId, status }));

    private void StubPropostaNaoEncontrada(Guid propostaId) =>
        _factory.PropostaApiStub
            .Given(Request.Create().WithPath($"/api/v1/propostas/{propostaId}").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(404));

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
    public async Task Criar_Com_Proposta_Aprovada_Retorna_201_E_Persiste_Contratacao()
    {
        var propostaId = Guid.NewGuid();
        StubProposta(propostaId, "Aprovada");

        var response = await _client.PostAsJsonAsync("/api/v1/contratacoes", CriarPayload(propostaId));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<ContratacaoDto>(JsonOptions);
        Assert.Equal(propostaId, dto!.PropostaId);
        Assert.StartsWith("AP-", dto.NumeroApolice, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Criar_Com_Papel_Usuario_Retorna_403()
    {
        using var clienteUsuario = _factory.CreateClient();
        clienteUsuario.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", JwtTestTokenFactory.CriarToken("usuario"));

        var propostaId = Guid.NewGuid();
        StubProposta(propostaId, "Aprovada");

        var response = await clienteUsuario.PostAsJsonAsync("/api/v1/contratacoes", CriarPayload(propostaId));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Criar_Com_Proposta_EmAnalise_Retorna_409()
    {
        var propostaId = Guid.NewGuid();
        StubProposta(propostaId, "EmAnalise");

        var response = await _client.PostAsJsonAsync("/api/v1/contratacoes", CriarPayload(propostaId));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Criar_Com_Proposta_Inexistente_Retorna_404()
    {
        var propostaId = Guid.NewGuid();
        StubPropostaNaoEncontrada(propostaId);

        var response = await _client.PostAsJsonAsync("/api/v1/contratacoes", CriarPayload(propostaId));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Criar_Repetido_Com_Mesma_Idempotency_Key_Retorna_A_Mesma_Contratacao()
    {
        var propostaId = Guid.NewGuid();
        StubProposta(propostaId, "Aprovada");
        var idempotencyKey = Guid.NewGuid().ToString();

        using var primeiraRequisicao = new HttpRequestMessage(HttpMethod.Post, "/api/v1/contratacoes")
        {
            Content = JsonContent.Create(CriarPayload(propostaId)),
        };
        primeiraRequisicao.Headers.Add("Idempotency-Key", idempotencyKey);
        var primeiraResposta = await _client.SendAsync(primeiraRequisicao);
        var primeiraContratacao = await primeiraResposta.Content.ReadFromJsonAsync<ContratacaoDto>(JsonOptions);

        using var segundaRequisicao = new HttpRequestMessage(HttpMethod.Post, "/api/v1/contratacoes")
        {
            Content = JsonContent.Create(CriarPayload(propostaId)),
        };
        segundaRequisicao.Headers.Add("Idempotency-Key", idempotencyKey);
        var segundaResposta = await _client.SendAsync(segundaRequisicao);
        var segundaContratacao = await segundaResposta.Content.ReadFromJsonAsync<ContratacaoDto>(JsonOptions);

        Assert.Equal(HttpStatusCode.Created, primeiraResposta.StatusCode);
        Assert.Equal(HttpStatusCode.Created, segundaResposta.StatusCode);
        Assert.Equal(primeiraContratacao!.Id, segundaContratacao!.Id);
        Assert.Equal(primeiraContratacao.NumeroApolice, segundaContratacao.NumeroApolice);
    }

    [Fact]
    public async Task Fluxo_Listar_ObterPorId_Atualizar_Cancelando_E_Deletar()
    {
        var propostaId = Guid.NewGuid();
        StubProposta(propostaId, "Aprovada");
        var criarResponse = await _client.PostAsJsonAsync("/api/v1/contratacoes", CriarPayload(propostaId));
        var criada = await criarResponse.Content.ReadFromJsonAsync<ContratacaoDto>(JsonOptions);

        var listarResponse = await _client.GetAsync("/api/v1/contratacoes");
        Assert.Equal(HttpStatusCode.OK, listarResponse.StatusCode);
        var lista = await listarResponse.Content.ReadFromJsonAsync<List<ContratacaoDto>>(JsonOptions);
        Assert.Contains(lista!, c => c.Id == criada!.Id);

        var obterResponse = await _client.GetAsync($"/api/v1/contratacoes/{criada!.Id}");
        Assert.Equal(HttpStatusCode.OK, obterResponse.StatusCode);

        var atualizarPayload = new
        {
            dataInicioVigencia = criada.DataInicioVigencia.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            dataFimVigencia = criada.DataFimVigencia.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            valorPremio = criada.ValorPremio,
            moedaValorPremio = criada.MoedaValorPremio,
            status = "Cancelada",
        };
        var atualizarResponse = await _client.PutAsJsonAsync($"/api/v1/contratacoes/{criada.Id}", atualizarPayload);
        Assert.Equal(HttpStatusCode.OK, atualizarResponse.StatusCode);
        var atualizada = await atualizarResponse.Content.ReadFromJsonAsync<ContratacaoDto>(JsonOptions);
        Assert.Equal(Domain.StatusContratacao.Cancelada, atualizada!.Status);

        var deletarResponse = await _client.DeleteAsync($"/api/v1/contratacoes/{criada.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deletarResponse.StatusCode);

        var obterAposDeletarResponse = await _client.GetAsync($"/api/v1/contratacoes/{criada.Id}");
        Assert.Equal(HttpStatusCode.NotFound, obterAposDeletarResponse.StatusCode);
    }

    [Fact]
    public async Task Requisicao_Sem_Token_Retorna_401()
    {
        using var clienteSemToken = _factory.CreateClient();

        var response = await clienteSemToken.GetAsync("/api/v1/contratacoes");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}

