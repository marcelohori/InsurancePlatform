using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Contratacao.Application.Exceptions;
using Contratacao.Application.Ports;
using Microsoft.AspNetCore.Http;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace Contratacao.Infrastructure.Http;

/// <summary>
/// Calls Proposta.Api's GET /propostas/{id} synchronously to check the proposal's status.
/// Resilience (retry, timeout, circuit breaker) is configured on the named HttpClient via
/// Microsoft.Extensions.Http.Resilience, not here - this adapter only translates the outcome.
/// </summary>
public sealed class HttpPropostaVerificationAdapter(HttpClient httpClient, IHttpContextAccessor httpContextAccessor)
    : IPropostaVerificationPort
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<PropostaVerificationResult?> VerificarAsync(Guid propostaId, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/propostas/{propostaId}");

        var incomingAuthorization = httpContextAccessor.HttpContext?.Request.Headers["Authorization"].ToString();
        if (!string.IsNullOrEmpty(incomingAuthorization))
        {
            if (ValidarAuthorizationHeader(incomingAuthorization))
            {
                request.Headers.TryAddWithoutValidation("Authorization", incomingAuthorization);
            }
        }

        HttpResponseMessage response;
        try
        {
            response = await httpClient.SendAsync(request, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or BrokenCircuitException or TimeoutRejectedException)
        {
            throw new PropostaServiceIndisponivelException("Serviço de propostas está indisponível no momento.", ex);
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new PropostaServiceIndisponivelException(
                "Serviço de propostas retornou uma resposta inválida.",
                new HttpRequestException($"Resposta HTTP inesperada: {(int)response.StatusCode} ({response.ReasonPhrase})."));
        }

        PropostaApiResponse? payload;
        try
        {
            payload = await response.Content.ReadFromJsonAsync<PropostaApiResponse>(JsonOptions, cancellationToken);
        }
        catch (JsonException ex)
        {
            throw new PropostaServiceIndisponivelException("Resposta inválida do serviço de propostas.", ex);
        }

        if (payload is null || payload.Id != propostaId || string.IsNullOrWhiteSpace(payload.Status))
        {
            throw new PropostaServiceIndisponivelException(
                "Resposta inválida do serviço de propostas.",
                new InvalidOperationException("Payload de proposta incompleto ou inconsistente."));
        }

        return payload is null ? null : new PropostaVerificationResult(payload.Id, payload.Status);
    }

    private static bool ValidarAuthorizationHeader(string header)
    {
        const int MaxHeaderLength = 8192;
        const int MaxTokenLength = 4096;

        if (header.Length > MaxHeaderLength)
        {
            return false;
        }

        var bearerMatch = Regex.Match(header, @"^Bearer\s+(.+)$", RegexOptions.IgnoreCase);
        if (!bearerMatch.Success)
        {
            return false;
        }

        var token = bearerMatch.Groups[1].Value;
        return !string.IsNullOrWhiteSpace(token) && token.Length <= MaxTokenLength;
    }

    private sealed record PropostaApiResponse(Guid Id, string Status);
}
