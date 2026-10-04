using FluentValidation;
using NogVita.Application.Common.Validation;

namespace NogVita.Application.Auth;

public sealed class LogoutRequestValidator : AbstractValidator<LogoutRequest>
{
    public LogoutRequestValidator()
    {
        RuleFor(x => x.RefreshToken).ValidRefreshToken();
    }
}