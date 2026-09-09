using Analise.Application.Ports;
using Analise.Application.UseCases;
using BuildingBlocks.Contracts.Events;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Analise.Infrastructure.Messaging;

/// <summary>
/// Triggers the risk assessment whenever Proposta.Api publishes PropostaCriadaEvent.
/// Deduplication of redelivered messages is handled by the EF inbox (UseEntityFrameworkOutbox
/// in the bus configuration), not by this consumer.
/// </summary>
public sealed class PropostaCriadaEventConsumer(
    ProcessarAnaliseUseCase processarAnaliseUseCase,
    ILogger<PropostaCriadaEventConsumer> logger) : IConsumer<PropostaCriadaEvent>
{
    public async Task Consume(ConsumeContext<PropostaCriadaEvent> context)
    {
        var evento = context.Message;

        using var scope = logger.BeginScope(new Dictionary<string, object>
        {
            ["PropostaId"] = evento.PropostaId,
            ["MessageId"] = context.MessageId ?? Guid.Empty,
        });
            ConsumerLog.Iniciando(logger, null);

        var input = new RiskAssessmentInput(
            evento.PropostaId,
            evento.TipoSeguro,
            evento.ValorCobertura,
            evento.ValorPremio);

        try
        {
            await processarAnaliseUseCase.ExecutarAsync(input, context.CancellationToken);
            ConsumerLog.Sucesso(logger, null);
        }
        catch (Exception exception)
        {
            ConsumerLog.Falha(logger, exception);
            throw;
        }
    }

    private static partial class ConsumerLog
    {
        private static readonly Action<ILogger, Exception?> IniciandoLog =
            LoggerMessage.Define(LogLevel.Information, new EventId(1, "PropostaCriadaEventIniciando"), "Iniciando processamento de PropostaCriadaEvent.");

        private static readonly Action<ILogger, Exception?> SucessoLog =
            LoggerMessage.Define(LogLevel.Information, new EventId(2, "PropostaCriadaEventSucesso"), "PropostaCriadaEvent processado com sucesso.");

        private static readonly Action<ILogger, Exception?> FalhaLog =
            LoggerMessage.Define(LogLevel.Error, new EventId(3, "PropostaCriadaEventFalha"), "Falha no processamento de PropostaCriadaEvent; mensagem será encaminhada para a fila de erro após as tentativas configuradas.");

        public static void Iniciando(ILogger logger, Exception? exception) => IniciandoLog(logger, exception);

        public static void Sucesso(ILogger logger, Exception? exception) => SucessoLog(logger, exception);

        public static void Falha(ILogger logger, Exception exception) => FalhaLog(logger, exception);
    }
}
