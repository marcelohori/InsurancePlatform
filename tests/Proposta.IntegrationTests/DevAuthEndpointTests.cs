using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Proposta.IntegrationTests;

[Collection(nameof(PropostaApiCollection))]
public sealed class DevAuthEndpointTests(PropostaApiFactory factory)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GerarToken_RoleValida_RetornaTokenQueAutenticaEndpointProtegido()
    {
        var response = await _client.PostAsJsonAsync("/api/dev/auth/token", new { usuarioId = "dev-user-1", role = "analista" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<TokenResponse>(JsonOptions);
        Assert.False(string.IsNullOrWhiteSpace(payload?.Token));

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", payload!.Token);
        var propostasResponse = await _client.GetAsync("/api/v1.0/propostas");

        Assert.Equal(HttpStatusCode.OK, propostasResponse.StatusCode);
    }

    [Fact]
    public async Task GerarToken_RoleInvalida_Retorna400()
    {
        var response = await _client.PostAsJsonAsync("/api/dev/auth/token", new { usuarioId = "dev-user-1", role = "super-admin" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GerarToken_SemBody_UsaValoresPadrao()
    {
        var response = await _client.PostAsJsonAsync("/api/dev/auth/token", (object?)null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<TokenResponse>(JsonOptions);
        Assert.Equal("usuario", payload?.Role);
    }

    private sealed record TokenResponse(string Token, string UsuarioId, string Role, DateTime ExpiresAt);
}
