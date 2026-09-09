using Analise.Application.Dtos;
using Analise.Application.Exceptions;
using Analise.Application.UseCases;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Analise.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/propostas")]
[Authorize]
public sealed class AnalisesController(ConsultarAnalisePorPropostaUseCase consultarUseCase) : ControllerBase
{
    [HttpGet("{propostaId:guid}/analise")]
    [ProducesResponseType<AnaliseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AnaliseDto>> ObterPorPropostaId(Guid propostaId, CancellationToken cancellationToken)
    {
        var dto = await consultarUseCase.ExecutarAsync(propostaId, cancellationToken)
            ?? throw new AnaliseNaoEncontradaException(propostaId);

        return Ok(dto);
    }
}
