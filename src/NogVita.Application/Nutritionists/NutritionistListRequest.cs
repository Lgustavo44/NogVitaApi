using FluentValidation;
using NogVita.Application.Common.Pagination;

namespace NogVita.Application.Nutritionists;

public sealed record NutritionistListRequest : PageRequest
{
    public string? Search { get; init; }
    public int? CrnRegion { get; init; }
}

public sealed class NutritionistListRequestValidator : PageRequestValidator<NutritionistListRequest>
{
    public NutritionistListRequestValidator()
    {
        RuleFor(r => r.Search).MaximumLength(100);
        RuleFor(r => r.CrnRegion).GreaterThan(0).When(r => r.CrnRegion is not null);
    }
}

public sealed record NutritionistListItem(
    Guid UserId,
    string Name,
    int CrnRegion,
    string CrnNumber,
    string? Bio);