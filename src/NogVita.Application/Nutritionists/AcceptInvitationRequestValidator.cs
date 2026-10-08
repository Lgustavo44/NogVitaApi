using FluentValidation;
using NogVita.Application.Common.Validation;

namespace NogVita.Application.Nutritionists;

public sealed class AcceptInvitationRequestValidator : AbstractValidator<AcceptInvitationRequest>
{
    public AcceptInvitationRequestValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("O token do convite é obrigatório.")
            .MaximumLength(128).WithMessage("O token do convite é inválido.");

        RuleFor(x => x.Password!)
            .StrongPassword()
            .When(x => !string.IsNullOrEmpty(x.Password));
    }
}