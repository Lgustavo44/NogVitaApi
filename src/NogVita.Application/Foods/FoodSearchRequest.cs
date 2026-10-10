using FluentValidation;
using NogVita.Application.Common.Pagination;
using NogVita.Domain.Foods;

namespace NogVita.Application.Foods;

public sealed record FoodSearchRequest : PageRequest
{
    public string Search { get; init; } = string.Empty;
    public FoodSource? Source { get; init; }
}

public sealed class FoodSearchRequestValidator : PageRequestValidator<FoodSearchRequest>
{
    public FoodSearchRequestValidator()
    {
        RuleFor(r => r.Search)
            .Must(s => !string.IsNullOrWhiteSpace(s) && s.Trim().Length >= 2)
            .WithMessage("Digite pelo menos 2 caracteres para buscar.")
            .MaximumLength(100);

        RuleFor(r => r.Source).IsInEnum().When(r => r.Source is not null);
    }
}