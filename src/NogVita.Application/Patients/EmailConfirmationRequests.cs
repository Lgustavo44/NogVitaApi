using FluentValidation;

namespace NogVita.Application.Patients;

public sealed record ConfirmEmailRequest(string Token);

public sealed record ResendEmailConfirmationRequest(string Email);

public sealed class ConfirmEmailRequestValidator : AbstractValidator<ConfirmEmailRequest>
{
    public ConfirmEmailRequestValidator()
    {
        RuleFor(r => r.Token).NotEmpty().MaximumLength(200);
    }
}

public sealed class ResendEmailConfirmationRequestValidator : AbstractValidator<ResendEmailConfirmationRequest>
{
    public ResendEmailConfirmationRequestValidator()
    {
        RuleFor(r => r.Email).NotEmpty().EmailAddress().MaximumLength(254);
    }
}