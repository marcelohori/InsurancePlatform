using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Proposta.Application.Contracts;
using Proposta.Application.Dtos;
using Xunit;

namespace Proposta.IntegrationTests;

[Collection(nameof(PropostaApiCollection))]
public sealed class InputValidationTests(PropostaApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    private void SetupAuth()
    {
        var token = PropostaApiFactory.GerarTokenAcesso("test-user", "usuario");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    [Fact]
    public async Task CriarProposta_NomeSeguradoVazio_Retorna400()
    {
        SetupAuth();
        var request = new { NomeSegurado = "", DocumentoSegurado = "11144477735", TipoSeguro = "Vida", ValorCobertura = 50000, ValorPremio = 1200 };

        var response = await _client.PostAsJsonAsync("/api/v1.0/propostas", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CriarProposta_DocumentoInvalido_Retorna400()
    {
        SetupAuth();
        var request = new { NomeSegurado = "João Silva", DocumentoSegurado = "invalid", TipoSeguro = "Vida", ValorCobertura = 50000, ValorPremio = 1200 };

        var response = await _client.PostAsJsonAsync("/api/v1.0/propostas", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CriarProposta_TipoSeguroInvalido_Retorna400()
    {
        SetupAuth();
        var request = new { NomeSegurado = "João Silva", DocumentoSegurado = "11144477735", TipoSeguro = "InvalidType", ValorCobertura = 50000, ValorPremio = 1200 };

        var response = await _client.PostAsJsonAsync("/api/v1.0/propostas", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CriarProposta_ValorCoberturaZero_Retorna400()
    {
        SetupAuth();
        var request = new { NomeSegurado = "João Silva", DocumentoSegurado = "11144477735", TipoSeguro = "Vida", ValorCobertura = 0, ValorPremio = 1200 };

        var response = await _client.PostAsJsonAsync("/api/v1.0/propostas", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CriarProposta_ValorCoberturaComMuitasCasasDecimais_Retorna400()
    {
        SetupAuth();
        var request = new { NomeSegurado = "João Silva", DocumentoSegurado = "11144477735", TipoSeguro = "Vida", ValorCobertura = 50000.999, ValorPremio = 1200 };

        var response = await _client.PostAsJsonAsync("/api/v1.0/propostas", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CriarProposta_MoedaInvalida_Retorna400()
    {
        SetupAuth();
        var request = new { NomeSegurado = "João Silva", DocumentoSegurado = "11144477735", TipoSeguro = "Vida", ValorCobertura = 50000, MoedaCobertura = "INVALID", ValorPremio = 1200 };

        var response = await _client.PostAsJsonAsync("/api/v1.0/propostas", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CriarProposta_NomeSeguradoOversized_Retorna400()
    {
        SetupAuth();
        var oversizedName = new string('A', 501);
        var request = new { NomeSegurado = oversizedName, DocumentoSegurado = "11144477735", TipoSeguro = "Vida", ValorCobertura = 50000, ValorPremio = 1200 };

        var response = await _client.PostAsJsonAsync("/api/v1.0/propostas", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static async Task<T?> ReadAsAsync<T>(HttpContent content)
    {
        var json = await content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(json);
    }
}
