using Analise.Application.Ports;

namespace Analise.UnitTests.UseCases;

public sealed class FakeUnitOfWork : IUnitOfWork
{
    public int VezesSalvo { get; private set; }

    public Task SalvarAlteracoesAsync(CancellationToken cancellationToken)
    {
        VezesSalvo++;
        return Task.CompletedTask;
    }
}

