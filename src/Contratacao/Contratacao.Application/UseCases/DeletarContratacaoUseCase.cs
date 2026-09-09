using Contratacao.Application.Exceptions;
using Contratacao.Application.Ports;

namespace Contratacao.Application.UseCases;

public sealed class DeletarContratacaoUseCase(IContratacaoRepository repositorio, IUnitOfWork unitOfWork)
{
    public async Task ExecutarAsync(Guid id, CancellationToken cancellationToken)
    {
        var apolice = await repositorio.ObterPorIdAsync(id, cancellationToken)
            ?? throw new ContratacaoNaoEncontradaException(id);

        repositorio.Remover(apolice);
        await unitOfWork.SalvarAlteracoesAsync(cancellationToken);
    }
}
