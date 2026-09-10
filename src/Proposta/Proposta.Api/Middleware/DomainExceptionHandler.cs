using BuildingBlocks.Contracts.Errors;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Proposta.Application.Exceptions;
using Proposta.Domain.Exceptions;
using Serilog;

namespace Proposta.Api.Middleware;

/// <summary>
/// Translates domain/application exceptions into the shared Problem Details error contract,
/// so a caller never sees a stack trace or a framework-specific error shape.
/// </summary>
public sealed class DomainExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (statusCode, type, title, detail) = exception switch
        {
            PropostaNaoEncontradaException => (StatusCodes.Status404NotFound, ProblemTypes.NotFound, "Recurso não encontrado", exception.Message),
            ConflictDomainException => (StatusCodes.Status409Conflict, ProblemTypes.Conflict, "Conflito de estado", exception.Message),
            DomainException => (StatusCodes.Status400BadRequest, ProblemTypes.ValidationError, "Dado inválido", exception.Message),
            _ => (0, string.Empty, string.Empty, string.Empty),
        };

        if (statusCode == 0)
        {
            Log.Error(exception, "Erro inesperado ao processar a requisição {Method} {Path}.",
                httpContext.Request.Method, httpContext.Request.Path);

            statusCode = StatusCodes.Status500InternalServerError;
            type = "https://httpstatuses.com/500";
            title = "Erro interno do servidor";
            detail = "Ocorreu um erro inesperado ao processar a requisição.";
        }

        httpContext.Response.StatusCode = statusCode;

        // Written directly as JSON (not via IProblemDetailsService.TryWriteAsync) so the client
        // always gets the real error detail. The negotiated writer silently drops Detail when
        // the request's Accept header doesn't include application/json - e.g. Swagger UI sends
        // "Accept: text/plain" by default, which made every error response come back as a bare
        // {type,title,status} with no explanation of what actually went wrong.
        await httpContext.Response.WriteAsJsonAsync(
            new ProblemDetails
            {
                Status = statusCode,
                Type = type,
                Title = title,
                Detail = detail,
            },
            options: null,
            contentType: "application/problem+json",
            cancellationToken: cancellationToken);

        return true;
    }
}
