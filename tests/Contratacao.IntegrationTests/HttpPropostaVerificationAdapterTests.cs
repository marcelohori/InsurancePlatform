using System.Net;
using Contratacao.Infrastructure.Http;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Contratacao.IntegrationTests;

public sealed class HttpPropostaVerificationAdapterTests
{
    [Fact]
    public async Task VerificarAsync_Propaga_Header_Authorization_Recebido()
    {
        var handler = new CapturingHandler();
        using var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://proposta.internal"),
        };
        var httpContextAccessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext(),
        };
        httpContextAccessor.HttpContext.Request.Headers.Authorization = "Bearer token-de-teste";

        var propostaId = Guid.NewGuid();
        var adapter = new HttpPropostaVerificationAdapter(client, httpContextAccessor);
        var resultado = await adapter.VerificarAsync(propostaId, CancellationToken.None);

        Assert.NotNull(resultado);
        Assert.Equal(propostaId, resultado.Id);
        Assert.Equal("Bearer token-de-teste", handler.Authorization);
    }

    [Fact]
    public async Task VerificarAsync_Sem_Header_Authorization_Nao_Adiciona_Credencial()
    {
        var handler = new CapturingHandler();
        using var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://proposta.internal"),
        };
        var adapter = new HttpPropostaVerificationAdapter(
            client,
            new HttpContextAccessor { HttpContext = new DefaultHttpContext() });

        await adapter.VerificarAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Null(handler.Authorization);
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        public string? Authorization { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Authorization = request.Headers.Authorization?.ToString();
            var propostaId = request.RequestUri!.Segments[^1];
            var content = $"{{\"id\":\"{propostaId}\",\"status\":\"Aprovada\"}}";
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(content),
            });
        }
    }
}
