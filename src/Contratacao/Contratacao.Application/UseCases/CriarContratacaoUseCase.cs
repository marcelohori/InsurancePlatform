using BuildingBlocks.Contracts.Events;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Contratacao.Application.Dtos;
using Contratacao.Application.Exceptions;
using Contratacao.Application.Ports;
using Contratacao.Domain;
using Contratacao.Domain.Exceptions;
using Contratacao.Domain.ValueObjects;

namespace Contratacao.Application.UseCases;

public sealed class CriarContratacaoUseCase(
    IContratacaoRepository repositorio,
    IPropostaVerificationPort propostaVerificationPort,
    IIdempotencyStore idempotencyStore,
    IEventPublisher publicadorDeEventos,
    IUnitOfWork unitOfWork)
{
    private const string StatusPropostaAprovada = "Aprovada";

    public async Task<ContratacaoDto> ExecutarAsync(CriarContratacaoCommand comando, CancellationToken cancellationToken)
    {
        var hashRequisicao = CalcularHashRequisicao(comando);

        if (!string.IsNullOrWhiteSpace(comando.IdempotencyKey))
        {
            var registroExistente = await idempotencyStore.ObterAsync(comando.IdempotencyKey, cancellationToken);
            if (registroExistente is not null)
            {
                if (!string.Equals(registroExistente.HashRequisicao, hashRequisicao, StringComparison.Ordinal))
                {
                    throw new ConflictDomainException("A chave de idempotência já foi utilizada com outro conteúdo.");
                }

                var existente = await repositorio.ObterPorIdAsync(registroExistente.ContratacaoId, cancellationToken);
                if (existente is not null)
                {
                    return ContratacaoDto.DeEntidade(existente);
                }
            }
        }

        var verificacao = await propostaVerificationPort.VerificarAsync(comando.PropostaId, cancellationToken)
            ?? throw new PropostaNaoEncontradaException(comando.PropostaId);

        if (verificacao.Status != StatusPropostaAprovada)
        {
            throw new PropostaNaoAprovadaException(comando.PropostaId, verificacao.Status);
        }

        var valorPremio = Monetario.Criar(comando.ValorPremio, comando.MoedaValorPremioOuPadrao);
        var vigencia = comando.DataInicioVigencia is not null && comando.DataFimVigencia is not null
            ? Vigencia.Criar(comando.DataInicioVigencia.Value, comando.DataFimVigencia.Value)
            : null;

        var apolice = ApoliceSeguro.Criar(comando.PropostaId, comando.DataContratacao, valorPremio, vigencia);

        repositorio.Adicionar(apolice);

        if (!string.IsNullOrWhiteSpace(comando.IdempotencyKey))
        {
            idempotencyStore.Registrar(comando.IdempotencyKey, apolice.Id, hashRequisicao);
        }

        await publicadorDeEventos.PublicarAsync(
            new ContratacaoEfetuadaEvent(apolice.Id, apolice.PropostaId, apolice.NumeroApolice, apolice.DataContratacao.ToDateTime(TimeOnly.MinValue)),
            cancellationToken);

        try
        {
            await unitOfWork.SalvarAlteracoesAsync(cancellationToken);
        }
        catch (ConflictDomainException) when (!string.IsNullOrWhiteSpace(comando.IdempotencyKey))
        {
            var registroExistente = await idempotencyStore.ObterAsync(comando.IdempotencyKey, cancellationToken)
                ?? throw new ConflictDomainException("A contratação concorrente ainda não está disponível.");

            if (!string.Equals(registroExistente.HashRequisicao, hashRequisicao, StringComparison.Ordinal))
            {
                throw new ConflictDomainException("A chave de idempotência já foi utilizada com outro conteúdo.");
            }

            var existente = await repositorio.ObterPorIdAsync(registroExistente.ContratacaoId, cancellationToken)
                ?? throw new ConflictDomainException("A contratação associada à chave de idempotência não foi encontrada.");

            return ContratacaoDto.DeEntidade(existente);
        }

        return ContratacaoDto.DeEntidade(apolice);
    }

    private static string CalcularHashRequisicao(CriarContratacaoCommand comando)
    {
        var payload = JsonSerializer.Serialize(comando);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash);
    }
}
