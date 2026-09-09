using BuildingBlocks.Contracts.Events;
using Proposta.Application.Dtos;
using Proposta.Application.Ports;
using Proposta.Domain;
using Proposta.Domain.Exceptions;
using Proposta.Domain.ValueObjects;

namespace Proposta.Application.UseCases;

public sealed class CriarPropostaUseCase(
    IPropostaRepository repositorio,
    IEventPublisher publicadorDeEventos,
    IUnitOfWork unitOfWork)
{
    public async Task<PropostaDto> ExecutarAsync(CriarPropostaCommand comando, CancellationToken cancellationToken)
    {
        var documento = DocumentoIdentificacao.Criar(comando.DocumentoSegurado);
        var tipoSeguro = ConverterTipoSeguro(comando.TipoSeguro);
        var valorCobertura = Monetario.Criar(comando.ValorCobertura, comando.MoedaCoberturaOuPadrao);
        var valorPremio = Monetario.Criar(comando.ValorPremio, comando.MoedaPremioOuPadrao);

        var proposta = PropostaSeguro.Criar(comando.NomeSegurado, documento, tipoSeguro, valorCobertura, valorPremio, criadoPor: comando.CriadoPor);

        repositorio.Adicionar(proposta);

        await publicadorDeEventos.PublicarAsync(
            new PropostaCriadaEvent(
                proposta.Id,
                proposta.NomeSegurado,
                proposta.DocumentoSegurado.Numero,
                proposta.TipoSeguro.ToString(),
                proposta.ValorCobertura.Quantia,
                proposta.ValorCobertura.Moeda,
                proposta.ValorPremio.Quantia,
                proposta.ValorPremio.Moeda,
                proposta.DataCriacao),
            cancellationToken);

        // Staged entity add and staged outbox event commit together in one transaction.
        await unitOfWork.SalvarAlteracoesAsync(cancellationToken);

        return PropostaDto.DeEntidade(proposta);
    }

    private static TipoSeguro ConverterTipoSeguro(string valor)
    {
        if (!Enum.TryParse<TipoSeguro>(valor, ignoreCase: true, out var tipoSeguro)
            || !Enum.IsDefined(tipoSeguro))
        {
            throw new DomainException($"Tipo de seguro inválido: '{valor}'.");
        }

        return tipoSeguro;
    }
}
