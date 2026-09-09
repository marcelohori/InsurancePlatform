using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Proposta.Application.Contracts;
using Proposta.Application.Dtos;
using Proposta.Domain;
using Proposta.Domain.ValueObjects;
using Xunit;

namespace Proposta.IntegrationTests;

[Collection(nameof(PropostaApiCollection))]
public sealed class AuthorizationBypassTests(PropostaApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task DeletarProposta_OutroUsuario_Retorna404()
    {
        // Arrange: Create proposta as user1
        var user1Token = PropostaApiFactory.GerarTokenAcesso("user-1", "usuario");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user1Token);

        var createRequest = new { NomeSegurado = "João", DocumentoSegurado = "11144477735", TipoSeguro = "Vida", ValorCobertura = 50000, ValorPremio = 1200 };
        var createResponse = await _client.PostAsJsonAsync("/api/v1.0/propostas", createRequest);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var proposta = (await ReadAsAsync<PropostaDto>(createResponse.Content))!;

        // Act: Try to delete as user2 (different user)
        var user2Token = PropostaApiFactory.GerarTokenAcesso("user-2", "usuario");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user2Token);

        var deleteResponse = await _client.DeleteAsync($"/api/v1.0/propostas/{proposta.Id}");

        // Assert: Should return 404 (not found for this user)
        Assert.Equal(HttpStatusCode.NotFound, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task AtualizarProposta_UsuarioComaloRoleAnalista_Retorna200()
    {
        // Arrange: Create proposta
        var userToken = PropostaApiFactory.GerarTokenAcesso("user-1", "usuario");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userToken);

        var createRequest = new { NomeSegurado = "João", DocumentoSegurado = "11144477735", TipoSeguro = "Vida", ValorCobertura = 50000, ValorPremio = 1200 };
        var createResponse = await _client.PostAsJsonAsync("/api/v1.0/propostas", createRequest);
        var proposta = (await ReadAsAsync<PropostaDto>(createResponse.Content))!;

        // Act: Update as analista (who has PropostaGerenciar policy)
        var analistaToken = PropostaApiFactory.GerarTokenAcesso("analyst-1", "analista");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", analistaToken);

        var updateRequest = new { NomeSegurado = "Maria", DocumentoSegurado = "11144477735", TipoSeguro = "Vida", ValorCobertura = 60000, ValorPremio = 1300, Status = "EmAnalise" };
        var updateResponse = await _client.PutAsJsonAsync($"/api/v1.0/propostas/{proposta.Id}", updateRequest);

        // Assert: Analista can update
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
    }

    [Fact]
    public async Task AtualizarProposta_UsuarioComRoleUsuario_Retorna403()
    {
        // Arrange: Create proposta
        var user1Token = PropostaApiFactory.GerarTokenAcesso("user-1", "usuario");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user1Token);

        var createRequest = new { NomeSegurado = "João", DocumentoSegurado = "11144477735", TipoSeguro = "Vida", ValorCobertura = 50000, ValorPremio = 1200 };
        var createResponse = await _client.PostAsJsonAsync("/api/v1.0/propostas", createRequest);
        var proposta = (await ReadAsAsync<PropostaDto>(createResponse.Content))!;

        // Act: Try to update as usuario (only has PropostaCriar, not PropostaGerenciar)
        var user2Token = PropostaApiFactory.GerarTokenAcesso("user-2", "usuario");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user2Token);

        var updateRequest = new { NomeSegurado = "Maria", DocumentoSegurado = "11144477735", TipoSeguro = "Vida", ValorCobertura = 60000, ValorPremio = 1300, Status = "EmAnalise" };
        var updateResponse = await _client.PutAsJsonAsync($"/api/v1.0/propostas/{proposta.Id}", updateRequest);

        // Assert: Usuario cannot update (403 Forbidden)
        Assert.Equal(HttpStatusCode.Forbidden, updateResponse.StatusCode);
    }

    private static async Task<T?> ReadAsAsync<T>(HttpContent content)
    {
        var json = await content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(json);
    }
}
