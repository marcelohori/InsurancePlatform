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
public sealed class DomainExceptionHandler(IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not PropostaNaoEncontradaException
            and not ConflictDomainException
            and not DomainException)
        {
            Log.Error(exception, "Erro inesperado ao processar a requisição {Method} {Path}.",
                httpContext.Request.Method, httpContext.Request.Path);

            httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
            return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = httpContext,
                ProblemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Type = "https://httpstatuses.com/500",
                    Title = "Erro interno do servidor",
                    Detail = "Ocorreu um erro inesperado ao processar a requisição.",
                },
            });
        }

        var (statusCode, type, title) = exception switch
        {
            PropostaNaoEncontradaException => (StatusCodes.Status404NotFound, ProblemTypes.NotFound, "Recurso não encontrado"),
            ConflictDomainException => (StatusCodes.Status409Conflict, ProblemTypes.Conflict, "Conflito de estado"),
            DomainException => (StatusCodes.Status400BadRequest, ProblemTypes.ValidationError, "Dado inválido"),
            _ => (0, string.Empty, string.Empty),
        };

        if (statusCode == 0)
        {
            return false;
        }

        httpContext.Response.StatusCode = statusCode;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = statusCode,
                Type = type,
                Title = title,
                Detail = exception is DomainException ? exception.Message : title,
            },
        });
    }
}
