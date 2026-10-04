using FluentValidation;

namespace NogVita.Application.Common.Validation;

public static class PasswordRules
{
    public const int MinLength = 15;
    public const int MaxLength = 128;

    private static readonly HashSet<string> Blocklist = new(StringComparer.OrdinalIgnoreCase)
    {
        "123456789012345",
        "1234567890123456",
        "senhasenhasenha",
        "passwordpassword",
        "qwertyuiopasdfgh",
        "nogvitanogvita123"
    };

    public static IRuleBuilderOptions<T, string> StrongPassword<T>(this IRuleBuilder<T, string> rule)
    {
        return rule
            .NotEmpty().WithMessage("A senha é obrigatória.")
            .MinimumLength(MinLength).WithMessage($"A senha precisa ter pelo menos {MinLength} caracteres.")
            .MaximumLength(MaxLength).WithMessage($"A senha pode ter no máximo {MaxLength} caracteres.")
            .Must(password => !Blocklist.Contains(password)).WithMessage("Essa senha é muito comum. Escolha outra.");
    }
}