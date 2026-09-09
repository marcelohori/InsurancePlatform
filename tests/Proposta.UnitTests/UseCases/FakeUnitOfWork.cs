using Proposta.Application.Ports;

namespace Proposta.UnitTests.UseCases;

public sealed class FakeUnitOfWork : IUnitOfWork
{
    public int VezesSalvo { get; private set; }

    public Task SalvarAlteracoesAsync(CancellationToken cancellationToken)
    {
        VezesSalvo++;
        return Task.CompletedTask;
    }
}
