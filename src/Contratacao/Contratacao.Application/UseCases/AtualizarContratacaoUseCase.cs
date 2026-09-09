using Contratacao.Application.Dtos;
using Contratacao.Application.Exceptions;
using Contratacao.Application.Ports;
using Contratacao.Domain;
using Contratacao.Domain.Exceptions;
using Contratacao.Domain.ValueObjects;

namespace Contratacao.Application.UseCases;

public sealed class AtualizarContratacaoUseCase(IContratacaoRepository repositorio, IUnitOfWork unitOfWork)
{
    public async Task<ContratacaoDto> ExecutarAsync(AtualizarContratacaoCommand comando, CancellationToken cancellationToken)
    {
        var apolice = await repositorio.ObterPorIdAsync(comando.Id, cancellationToken)
            ?? throw new ContratacaoNaoEncontradaException(comando.Id);

        if (comando.Status != apolice.Status && comando.Status != StatusContratacao.Cancelada)
        {
            throw new ConflictDomainException(
                $"Não é possível alterar a contratação {apolice.Id} para o status '{comando.Status}'.");
        }

        var valorPremio = Monetario.Criar(comando.ValorPremio, comando.MoedaValorPremioOuPadrao);
        var vigencia = Vigencia.Criar(comando.DataInicioVigencia, comando.DataFimVigencia);
        apolice.AtualizarDados(vigencia, valorPremio);

        if (comando.Status == StatusContratacao.Cancelada)
        {
            apolice.Cancelar();
        }

        repositorio.Atualizar(apolice);
        await unitOfWork.SalvarAlteracoesAsync(cancellationToken);

        return ContratacaoDto.DeEntidade(apolice);
    }
}
