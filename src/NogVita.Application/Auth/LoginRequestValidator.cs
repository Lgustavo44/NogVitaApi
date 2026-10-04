using FluentValidation;
using NogVita.Application.Common.Validation;

namespace NogVita.Application.Auth;

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("O e-mail é obrigatório.")
            .MaximumLength(254).WithMessage("O e-mail pode ter no máximo 254 caracteres.")
            .EmailAddress().WithMessage("O e-mail informado não é válido.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("A senha é obrigatória.")
            .MaximumLength(PasswordRules.MaxLength).WithMessage($"A senha pode ter no máximo {PasswordRules.MaxLength} caracteres.");
    }
}