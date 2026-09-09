using FluentValidation;
using Contratacao.Application.Contracts;
using Contratacao.Domain;

namespace Contratacao.Application.Validators;

public sealed class AtualizarContratacaoRequestValidator : AbstractValidator<AtualizarContratacaoRequest>
{
    public AtualizarContratacaoRequestValidator()
    {
        RuleFor(x => x.DataInicioVigencia)
            .NotEmpty().WithMessage("Data de início de vigência é obrigatória");

        RuleFor(x => x.DataFimVigencia)
            .GreaterThan(x => x.DataInicioVigencia)
            .WithMessage("Data de fim de vigência deve ser posterior à data de início");

        RuleFor(x => x.ValorPremio)
            .GreaterThan(0).WithMessage("Valor do prêmio deve ser maior que zero")
            .Must(HaveTwoDecimals).WithMessage("Valor do prêmio deve ter no máximo duas casas decimais");

        RuleFor(x => x.MoedaValorPremio)
            .Must(BeValidCurrency).WithMessage("Código de moeda inválido (deve ser 3 letras, ex: BRL)")
            .When(x => !string.IsNullOrEmpty(x.MoedaValorPremio));

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Status inválido");
    }

    private static bool HaveTwoDecimals(decimal value) =>
        decimal.Round(value, 2) == value;

    private static bool BeValidCurrency(string? currency) =>
        string.IsNullOrEmpty(currency) ||
        (currency.Length == 3 && currency.All(c => char.IsLetter(c)));
}
