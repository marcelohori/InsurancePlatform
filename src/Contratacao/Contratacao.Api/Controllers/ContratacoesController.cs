using Asp.Versioning;
using BuildingBlocks.Contracts.Authorization;
using Contratacao.Application.Contracts;
using Contratacao.Application.Dtos;
using Contratacao.Application.Exceptions;
using Contratacao.Application.UseCases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;

namespace Contratacao.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/contratacoes")]
[Authorize]
public sealed class ContratacoesController(
    CriarContratacaoUseCase criarUseCase,
    ListarContratacoesUseCase listarUseCase,
    ObterContratacaoPorIdUseCase obterPorIdUseCase,
    AtualizarContratacaoUseCase atualizarUseCase,
    DeletarContratacaoUseCase deletarUseCase) : ControllerBase
{
    private const string IdempotencyKeyHeader = "Idempotency-Key";
    private static readonly Regex UuidV4Regex = new(@"^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$", RegexOptions.IgnoreCase);

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.ContratacaoCriar)]
    [ProducesResponseType<ContratacaoDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ContratacaoDto>> Criar(CriarContratacaoRequest request, CancellationToken cancellationToken)
    {
        Request.Headers.TryGetValue(IdempotencyKeyHeader, out var idempotencyKey);
        var idempotencyKeyValue = idempotencyKey.FirstOrDefault();

        if (!string.IsNullOrEmpty(idempotencyKeyValue) && !UuidV4Regex.IsMatch(idempotencyKeyValue))
        {
            return BadRequest(new { error = "Idempotency-Key header deve estar em formato UUID v4 válido" });
        }

        var comando = new CriarContratacaoCommand(
            request.PropostaId,
            request.DataContratacao,
            request.DataInicioVigencia,
            request.DataFimVigencia,
            request.ValorPremio,
            request.MoedaValorPremio,
            idempotencyKeyValue);

        var dto = await criarUseCase.ExecutarAsync(comando, cancellationToken);
        return CreatedAtAction(nameof(ObterPorId), new { id = dto.Id, version = "1.0" }, dto);
    }

    [HttpGet]
    [Authorize]
    [ProducesResponseType<IReadOnlyList<ContratacaoDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ContratacaoDto>>> Listar(
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanhoPagina = 20,
        CancellationToken cancellationToken = default)
    {
        var contratacoes = await listarUseCase.ExecutarAsync(pagina, tamanhoPagina, cancellationToken);
        return Ok(contratacoes);
    }

    [HttpGet("{id:guid}")]
    [Authorize]
    [ProducesResponseType<ContratacaoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ContratacaoDto>> ObterPorId(Guid id, CancellationToken cancellationToken)
    {
        var dto = await obterPorIdUseCase.ExecutarAsync(id, cancellationToken)
            ?? throw new ContratacaoNaoEncontradaException(id);

        return Ok(dto);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.ContratacaoCriar)]
    [ProducesResponseType<ContratacaoDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ContratacaoDto>> Atualizar(Guid id, AtualizarContratacaoRequest request, CancellationToken cancellationToken)
    {
        var comando = new AtualizarContratacaoCommand(
            id,
            request.DataInicioVigencia,
            request.DataFimVigencia,
            request.ValorPremio,
            request.MoedaValorPremio,
            request.Status);

        var dto = await atualizarUseCase.ExecutarAsync(comando, cancellationToken);
        return Ok(dto);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.ContratacaoCriar)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Deletar(Guid id, CancellationToken cancellationToken)
    {
        await deletarUseCase.ExecutarAsync(id, cancellationToken);
        return NoContent();
    }
}
