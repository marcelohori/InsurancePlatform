using FluentValidation;
using Proposta.Application.Contracts;
using Proposta.Domain;

namespace Proposta.Application.Validators;

public sealed class AtualizarPropostaRequestValidator : AbstractValidator<AtualizarPropostaRequest>
{
    public AtualizarPropostaRequestValidator()
    {
        RuleFor(x => x.NomeSegurado)
            .NotEmpty().WithMessage("Nome do segurado é obrigatório")
            .MaximumLength(500).WithMessage("Nome do segurado deve ter no máximo 500 caracteres");

        RuleFor(x => x.DocumentoSegurado)
            .NotEmpty().WithMessage("Documento do segurado é obrigatório")
            .MaximumLength(20).WithMessage("Documento inválido");

        RuleFor(x => x.TipoSeguro)
            .NotEmpty().WithMessage("Tipo de seguro é obrigatório")
            .Must(BeValidTipoSeguro).WithMessage("Tipo de seguro inválido (Vida, Saude, Propriedade)");

        RuleFor(x => x.ValorCobertura)
            .GreaterThan(0).WithMessage("Valor de cobertura deve ser maior que zero")
            .Must(HaveTwoDecimals).WithMessage("Valor de cobertura deve ter no máximo duas casas decimais");

        RuleFor(x => x.MoedaCobertura)
            .Must(BeValidCurrency).WithMessage("Código de moeda inválido (deve ser 3 letras, ex: BRL)")
            .When(x => !string.IsNullOrEmpty(x.MoedaCobertura));

        RuleFor(x => x.ValorPremio)
            .GreaterThan(0).WithMessage("Valor do prêmio deve ser maior que zero")
            .Must(HaveTwoDecimals).WithMessage("Valor do prêmio deve ter no máximo duas casas decimais");

        RuleFor(x => x.MoedaPremio)
            .Must(BeValidCurrency).WithMessage("Código de moeda inválido (deve ser 3 letras, ex: BRL)")
            .When(x => !string.IsNullOrEmpty(x.MoedaPremio));

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Status inválido");
    }

    private static bool BeValidTipoSeguro(string tipoSeguro) =>
        !string.IsNullOrWhiteSpace(tipoSeguro) &&
        (tipoSeguro.Equals("Vida", StringComparison.OrdinalIgnoreCase) ||
         tipoSeguro.Equals("Saude", StringComparison.OrdinalIgnoreCase) ||
         tipoSeguro.Equals("Propriedade", StringComparison.OrdinalIgnoreCase));

    private static bool HaveTwoDecimals(decimal value) =>
        decimal.Round(value, 2) == value;

    private static bool BeValidCurrency(string? currency) =>
        string.IsNullOrEmpty(currency) ||
        (currency.Length == 3 && currency.All(c => char.IsLetter(c)));
}
