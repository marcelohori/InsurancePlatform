using Asp.Versioning;
using BuildingBlocks.Contracts.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Proposta.Application.Contracts;
using Proposta.Application.Dtos;
using Proposta.Application.Exceptions;
using Proposta.Application.UseCases;

namespace Proposta.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/propostas")]
[Authorize]
public sealed class PropostasController(
    CriarPropostaUseCase criarUseCase,
    ListarPropostasUseCase listarUseCase,
    ObterPropostaPorIdUseCase obterPorIdUseCase,
    AtualizarPropostaUseCase atualizarUseCase,
    DeletarPropostaUseCase deletarUseCase) : ControllerBase
{
    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.PropostaCriar)]
    [ProducesResponseType<PropostaDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<PropostaDto>> Criar(CriarPropostaCommand request, CancellationToken cancellationToken)
    {
        var usuarioId = ObterUsuarioId();
        if (usuarioId is null)
        {
            return Unauthorized();
        }

        var comando = request with { CriadoPor = usuarioId };
        var dto = await criarUseCase.ExecutarAsync(comando, cancellationToken);
        return CreatedAtAction(nameof(ObterPorId), new { id = dto.Id, version = "1.0" }, dto);
    }

    [HttpGet]
    [Authorize]
    [ProducesResponseType<IReadOnlyList<PropostaDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PropostaDto>>> Listar(
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanhoPagina = 20,
        CancellationToken cancellationToken = default)
    {
        var usuarioId = User.IsInRole("admin") || User.IsInRole("Admin")
            || User.IsInRole("analista") || User.IsInRole("Analista")
            ? null
            : ObterUsuarioId();

        if (usuarioId is null && !User.IsInRole("admin") && !User.IsInRole("Admin")
            && !User.IsInRole("analista") && !User.IsInRole("Analista"))
        {
            return Unauthorized();
        }

        var propostas = await listarUseCase.ExecutarAsync(pagina, tamanhoPagina, usuarioId, cancellationToken);
        return Ok(propostas);
    }

    [HttpGet("{id:guid}")]
    [Authorize]
    [ProducesResponseType<PropostaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PropostaDto>> ObterPorId(Guid id, CancellationToken cancellationToken)
    {
        var usuarioId = User.IsInRole("admin") || User.IsInRole("Admin")
            || User.IsInRole("analista") || User.IsInRole("Analista")
            ? null
            : ObterUsuarioId();

        var dto = await obterPorIdUseCase.ExecutarAsync(id, usuarioId, cancellationToken)
            ?? throw new PropostaNaoEncontradaException(id);

        return Ok(dto);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.PropostaGerenciar)]
    [ProducesResponseType<PropostaDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PropostaDto>> Atualizar(Guid id, AtualizarPropostaRequest request, CancellationToken cancellationToken)
    {
        var comando = new AtualizarPropostaCommand(
            id,
            request.NomeSegurado,
            request.DocumentoSegurado,
            request.TipoSeguro,
            request.ValorCobertura,
            request.MoedaCobertura,
            request.ValorPremio,
            request.MoedaPremio,
            request.Status);

        var dto = await atualizarUseCase.ExecutarAsync(comando, cancellationToken);
        return Ok(dto);
    }

    [HttpDelete("{id:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Deletar(Guid id, CancellationToken cancellationToken)
    {
        var usuarioId = User.IsInRole("admin") || User.IsInRole("Admin")
            || User.IsInRole("analista") || User.IsInRole("Analista")
            ? null
            : ObterUsuarioId();

        if (usuarioId is null && !User.IsInRole("admin") && !User.IsInRole("Admin")
            && !User.IsInRole("analista") && !User.IsInRole("Analista"))
        {
            return Unauthorized();
        }

        await deletarUseCase.ExecutarAsync(id, usuarioId, cancellationToken);
        return NoContent();
    }

    private string? ObterUsuarioId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User.FindFirstValue("sub")
        ?? User.Identity?.Name;
}
