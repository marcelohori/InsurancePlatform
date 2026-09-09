namespace Contratacao.Application.Ports;

/// <summary>
/// Outbound port for the synchronous REST call to Proposta.Api that checks whether a
/// proposal exists and its current status, before a contratação is allowed.
/// </summary>
public interface IPropostaVerificationPort
{
    Task<PropostaVerificationResult?> VerificarAsync(Guid propostaId, CancellationToken cancellationToken);
}

/// <param name="Status">The proposal's status, exactly as reported by Proposta.Api (e.g. "Aprovada").</param>
public sealed record PropostaVerificationResult(Guid Id, string Status);
