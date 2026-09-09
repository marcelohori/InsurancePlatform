using Analise.Domain;

namespace Analise.Application.Ports;

public interface IAnaliseRepository
{
    void Adicionar(AnaliseRisco analise);

    Task<AnaliseRisco?> ObterPorPropostaIdAsync(Guid propostaId, CancellationToken cancellationToken);

    void Atualizar(AnaliseRisco analise);
}
