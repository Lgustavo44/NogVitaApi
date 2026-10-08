using FluentValidation;
using NogVita.Domain.Users;

namespace NogVita.Application.Nutritionists;

public sealed class PreRegisterNutritionistRequestValidator : AbstractValidator<PreRegisterNutritionistRequest>
{
    private const int MinCrnRegion = 1;
    private const int MaxCrnRegion = 11;

    public PreRegisterNutritionistRequestValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("O nome é obrigatório.")
            .MaximumLength(150).WithMessage("O nome pode ter no máximo 150 caracteres.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("O e-mail é obrigatório.")
            .MaximumLength(254).WithMessage("O e-mail pode ter no máximo 254 caracteres.")
            .EmailAddress().WithMessage("O e-mail informado não é válido.");

        RuleFor(x => x.Cpf)
            .NotEmpty().WithMessage("O CPF é obrigatório.")
            .Must(Cpf.IsValid).WithMessage("O CPF informado não é válido.");

        RuleFor(x => x.CrnRegion)
            .InclusiveBetween(MinCrnRegion, MaxCrnRegion)
            .WithMessage($"A região do CRN precisa estar entre {MinCrnRegion} e {MaxCrnRegion}.");

        RuleFor(x => x.CrnNumber)
            .NotEmpty().WithMessage("O número do CRN é obrigatório.")
            .MaximumLength(20).WithMessage("O número do CRN pode ter no máximo 20 caracteres.");
    }
}