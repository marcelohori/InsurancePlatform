using BuildingBlocks.Contracts.Errors;
using Contratacao.Application.Exceptions;
using Contratacao.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace Contratacao.Api.Middleware;

/// <summary>Translates domain/application exceptions into the shared Problem Details error contract.</summary>
public sealed class DomainExceptionHandler(IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not ContratacaoNaoEncontradaException
            and not PropostaNaoEncontradaException
            and not PropostaNaoAprovadaException
            and not PropostaServiceIndisponivelException
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
            ContratacaoNaoEncontradaException => (StatusCodes.Status404NotFound, ProblemTypes.NotFound, "Recurso não encontrado"),
            PropostaNaoEncontradaException => (StatusCodes.Status404NotFound, ProblemTypes.NotFound, "Proposta não encontrada"),
            PropostaNaoAprovadaException => (StatusCodes.Status409Conflict, ProblemTypes.Conflict, "Proposta não aprovada"),
            PropostaServiceIndisponivelException => (StatusCodes.Status503ServiceUnavailable, ProblemTypes.ServiceUnavailable, "Serviço de propostas indisponível"),
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
