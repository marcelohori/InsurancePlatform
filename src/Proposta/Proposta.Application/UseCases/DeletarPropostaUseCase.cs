using Proposta.Application.Exceptions;
using Proposta.Application.Ports;

namespace Proposta.Application.UseCases;

public sealed class DeletarPropostaUseCase(IPropostaRepository repositorio, IUnitOfWork unitOfWork)
{
    public async Task ExecutarAsync(Guid id, string? criadoPor, CancellationToken cancellationToken)
    {
        var proposta = await repositorio.ObterPorIdAsync(id, criadoPor, cancellationToken)
            ?? throw new PropostaNaoEncontradaException(id);

        proposta.GarantirQuePodeSerExcluida();

        repositorio.Remover(proposta);
        await unitOfWork.SalvarAlteracoesAsync(cancellationToken);
    }
}
