using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Xunit;

namespace Proposta.IntegrationTests;

/// <summary>
/// Guards against a regression where DomainExceptionHandler used IProblemDetailsService's
/// content-negotiated writer: when a client sent "Accept: text/plain" (which is what Swagger
/// UI's "Try it out" sends by default), the JSON ProblemDetails writer silently declined to
/// write the body, so every error response came back as a bare {type,title,status} with the
/// real error message (Detail) missing - the client had no way to know what actually failed.
/// </summary>
[Collection(nameof(PropostaApiCollection))]
public sealed class ErrorResponseContentNegotiationTests(PropostaApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task CriarProposta_DocumentoInvalido_ComAcceptTextPlain_AindaRetornaDetalheDoErro()
    {
        var token = PropostaApiFactory.GerarTokenAcesso("test-user", "usuario");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        _client.DefaultRequestHeaders.Accept.Clear();
        _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));

        var request = new { NomeSegurado = "Joao Silva", DocumentoSegurado = "11144477736", TipoSeguro = "Auto", ValorCobertura = 50000, ValorPremio = 1200 };
        var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/api/v1.0/propostas", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(body);
        var detail = json.RootElement.GetProperty("detail").GetString();

        Assert.False(string.IsNullOrWhiteSpace(detail));
        Assert.Contains("Documento de identificação inválido", detail);
    }
}
