using FluentValidation;
using NogVita.Domain.Users;

namespace NogVita.Application.Nutritionists;

public sealed record UpdateMyNutritionistProfileRequest(string? Bio);

public sealed class UpdateMyNutritionistProfileRequestValidator : AbstractValidator<UpdateMyNutritionistProfileRequest>
{
    public UpdateMyNutritionistProfileRequestValidator()
    {
        RuleFor(r => r.Bio)
            .MaximumLength(NutritionistProfile.BioMaxLength)
            .WithMessage($"A apresentação pode ter no máximo {NutritionistProfile.BioMaxLength} caracteres.");
    }
}