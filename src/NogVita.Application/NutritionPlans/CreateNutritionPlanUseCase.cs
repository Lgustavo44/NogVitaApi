using FluentValidation;
using NogVita.Application.Abstractions;
using NogVita.Application.FollowUps;
using NogVita.Domain.NutritionPlans;

namespace NogVita.Application.NutritionPlans;

public sealed record CreateNutritionPlanRequest(
    Guid PatientId,
    string Name,
    string? Description,
    DateOnly StartDate,
    DateOnly? EndDate);

public sealed class CreateNutritionPlanRequestValidator : AbstractValidator<CreateNutritionPlanRequest>
{
    public CreateNutritionPlanRequestValidator()
    {
        RuleFor(r => r.PatientId).NotEmpty();
        RuleFor(r => r.Name).NotEmpty().MaximumLength(NutritionPlan.NameMaxLength);
        RuleFor(r => r.Description).MaximumLength(NutritionPlan.DescriptionMaxLength);
        RuleFor(r => r.EndDate)
            .GreaterThanOrEqualTo(r => r.StartDate)
            .When(r => r.EndDate is not null)
            .WithMessage("A data de fim não pode ser anterior à data de início.");
    }
}

public sealed class CreateNutritionPlanUseCase(
    CareAccessService careAccessService,
    INutritionPlanRepository planRepository,
    IUnitOfWork unitOfWork)
{
    public async Task<Guid?> ExecuteAsync(
        Guid nutritionistId,
        CreateNutritionPlanRequest request,
        CancellationToken cancellationToken = default)
    {
        var hasAccess = await careAccessService.NutritionistFollowsPatientAsync(nutritionistId, request.PatientId, cancellationToken);

        if (!hasAccess)
        {
            return null;
        }

        var plan = new NutritionPlan(
            request.PatientId,
            nutritionistId,
            request.Name,
            request.Description,
            request.StartDate,
            request.EndDate);

        planRepository.Add(plan);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return plan.Id;
    }
}