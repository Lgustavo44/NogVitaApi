using FluentValidation;

public static class RefreshTokenRules
{
    public const int MaxLength = 128;

    public static IRuleBuilderOptions<T, string> ValidRefreshToken<T>(this IRuleBuilder<T, string> rule)
    {
        return rule
            .NotEmpty().WithMessage("O refresh token é obrigatório.")
            .MaximumLength(MaxLength).WithMessage($"O refresh token pode ter no máximo {MaxLength} caracteres.");
    }
}