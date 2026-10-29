using FluentValidation;
using NogVita.Application.Common.Pagination;
using NogVita.Domain.NutritionPlans;

namespace NogVita.Application.NutritionPlans;

public sealed record NutritionPlanResponse(
    Guid Id,
    Guid PatientId,
    string PatientName,
    Guid NutritionistId,
    string NutritionistName,
    string Name,
    string? Description,
    DateOnly StartDate,
    DateOnly? EndDate,
    NutritionPlanStatus Status,
    DateTime CreatedAt,
    decimal? TargetEnergyKcal,
    decimal? TargetProteinGrams,
    decimal? TargetCarbohydrateGrams,
    decimal? TargetFatGrams);

public sealed record NutritionPlanListRequest : PageRequest
{
    public NutritionPlanStatus? Status { get; init; }
}

public sealed class NutritionPlanListRequestValidator : PageRequestValidator<NutritionPlanListRequest>
{
    public NutritionPlanListRequestValidator()
    {
        RuleFor(r => r.Status).IsInEnum().When(r => r.Status is not null);
    }
}
