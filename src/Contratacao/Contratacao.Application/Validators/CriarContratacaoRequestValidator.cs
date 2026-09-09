using FluentValidation;
using Contratacao.Application.Contracts;

namespace Contratacao.Application.Validators;

public sealed class CriarContratacaoRequestValidator : AbstractValidator<CriarContratacaoRequest>
{
    public CriarContratacaoRequestValidator()
    {
        RuleFor(x => x.PropostaId)
            .NotEmpty().WithMessage("PropostaId é obrigatório");

        RuleFor(x => x.DataContratacao)
            .NotEmpty().WithMessage("Data de contratação é obrigatória")
            .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Data de contratação não pode ser no futuro");

        RuleFor(x => x.DataInicioVigencia)
            .GreaterThanOrEqualTo(x => x.DataContratacao)
            .WithMessage("Data de início de vigência não pode ser anterior à data de contratação")
            .When(x => x.DataInicioVigencia.HasValue);

        RuleFor(x => x.DataFimVigencia)
            .GreaterThan(x => x.DataInicioVigencia ?? x.DataContratacao)
            .WithMessage("Data de fim de vigência deve ser posterior à data de início")
            .When(x => x.DataFimVigencia.HasValue);

        RuleFor(x => x.ValorPremio)
            .GreaterThan(0).WithMessage("Valor do prêmio deve ser maior que zero")
            .Must(HaveTwoDecimals).WithMessage("Valor do prêmio deve ter no máximo duas casas decimais");

        RuleFor(x => x.MoedaValorPremio)
            .Must(BeValidCurrency).WithMessage("Código de moeda inválido (deve ser 3 letras, ex: BRL)")
            .When(x => !string.IsNullOrEmpty(x.MoedaValorPremio));
    }

    private static bool HaveTwoDecimals(decimal value) =>
        decimal.Round(value, 2) == value;

    private static bool BeValidCurrency(string? currency) =>
        string.IsNullOrEmpty(currency) ||
        (currency.Length == 3 && currency.All(c => char.IsLetter(c)));
}
