using FluentValidation;

namespace NogVita.Application.Auth;

public sealed class RefreshRequestValidator : AbstractValidator<RefreshRequest>
{
    public RefreshRequestValidator()
    {
        RuleFor(x => x.RefreshToken).ValidRefreshToken();
    }
}