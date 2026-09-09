using Proposta.Application.Dtos;
using Proposta.Application.Exceptions;
using Proposta.Application.Ports;
using Proposta.Domain;
using Proposta.Domain.Exceptions;
using Proposta.Domain.ValueObjects;

namespace Proposta.Application.UseCases;

public sealed class AtualizarPropostaUseCase(IPropostaRepository repositorio, IUnitOfWork unitOfWork)
{
    public async Task<PropostaDto> ExecutarAsync(AtualizarPropostaCommand comando, CancellationToken cancellationToken)
    {
        var proposta = await repositorio.ObterPorIdAsync(comando.Id, null, cancellationToken)
            ?? throw new PropostaNaoEncontradaException(comando.Id);

        var statusMudou = comando.Status != proposta.Status;
        var dadosMudaram = comando.NomeSegurado != proposta.NomeSegurado
            || comando.DocumentoSegurado != proposta.DocumentoSegurado.Numero
            || comando.TipoSeguro != proposta.TipoSeguro.ToString()
            || comando.ValorCobertura != proposta.ValorCobertura.Quantia
            || comando.ValorPremio != proposta.ValorPremio.Quantia;

        if (dadosMudaram)
        {
            proposta.GarantirQuePodeSerAtualizada();
        }

        if (statusMudou)
        {
            switch (comando.Status)
            {
                case StatusProposta.Aprovada:
                    proposta.Aprovar();
                    break;
                case StatusProposta.Rejeitada:
                    proposta.Rejeitar();
                    break;
                default:
                    throw new ConflictDomainException(
                        $"Não é possível reverter a proposta {proposta.Id} para o status '{comando.Status}'.");
            }
        }

        if (dadosMudaram)
        {
            var documento = DocumentoIdentificacao.Criar(comando.DocumentoSegurado);
            var tipoSeguro = ConverterTipoSeguro(comando.TipoSeguro);
            var valorCobertura = Monetario.Criar(comando.ValorCobertura, comando.MoedaCoberturaOuPadrao);
            var valorPremio = Monetario.Criar(comando.ValorPremio, comando.MoedaPremioOuPadrao);

            proposta.AtualizarDados(comando.NomeSegurado, documento, tipoSeguro, valorCobertura, valorPremio);
        }

        repositorio.Atualizar(proposta);
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
