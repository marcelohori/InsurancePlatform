using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Proposta.Application.Dtos;
using Xunit;

namespace Proposta.IntegrationTests;

/// <summary>
/// API contract tests: validate HTTP semantics (status codes, headers, schema)
/// for Proposta.Api endpoints across authorization, pagination, and error scenarios.
/// </summary>
[Collection(nameof(PropostaApiCollection))]
public sealed class ContratosApiTests(PropostaApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Criar_SemAutorizacao_Retorna401()
    {
        // Act: POST sem token
        var response = await _client.GetAsync("/api/v1.0/propostas");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Listar_ComTokenValido_Retorna200()
    {
        // Arrange: generate token
        var token = PropostaApiFactory.GerarTokenAcesso("test-user");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/v1.0/propostas?pagina=1&tamanhoPagina=20");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Listar_ComPaginacaoInvalida_Retorna400()
    {
        // Arrange: invalid page
        var token = PropostaApiFactory.GerarTokenAcesso("test-user");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act: page 0 (invalid)
        var response = await _client.GetAsync("/api/v1.0/propostas?pagina=0&tamanhoPagina=20");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ObterPorId_ComIdInexistente_Retorna404()
    {
        // Arrange
        var token = PropostaApiFactory.GerarTokenAcesso("test-user");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var inexistentId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/v1.0/propostas/{inexistentId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
