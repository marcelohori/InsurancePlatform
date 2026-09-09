using Analise.Application.Exceptions;
using Analise.Application.Ports;

namespace Analise.UnitTests.UseCases;

public sealed class FakeRiskAssessmentPort : IRiskAssessmentPort
{
    private readonly RiskAssessmentResult? _resultado;
    private readonly Exception? _falha;

    private FakeRiskAssessmentPort(RiskAssessmentResult? resultado, Exception? falha)
    {
        _resultado = resultado;
        _falha = falha;
    }

    public static FakeRiskAssessmentPort ComSucesso(RiskAssessmentResult resultado) => new(resultado, null);

    public static FakeRiskAssessmentPort ComFalha(string mensagem) =>
        new(null, new RiskAssessmentIndisponivelException(mensagem, new InvalidOperationException(mensagem)));

    public Task<RiskAssessmentResult> AvaliarAsync(RiskAssessmentInput input, CancellationToken cancellationToken) =>
        _falha is not null ? Task.FromException<RiskAssessmentResult>(_falha) : Task.FromResult(_resultado!);
}

