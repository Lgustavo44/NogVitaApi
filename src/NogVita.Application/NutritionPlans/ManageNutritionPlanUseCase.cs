using FluentValidation;
using NogVita.Application.Abstractions;
using NogVita.Application.Common;
using NogVita.Application.FollowUps;
using NogVita.Domain.NutritionPlans;

namespace NogVita.Application.NutritionPlans;

public sealed record UpdateNutritionPlanRequest(
    string Name,
    string? Description,
    DateOnly StartDate,
    DateOnly? EndDate);

public sealed class UpdateNutritionPlanRequestValidator : AbstractValidator<UpdateNutritionPlanRequest>
{
    public UpdateNutritionPlanRequestValidator()
    {
        RuleFor(r => r.Name).NotEmpty().MaximumLength(NutritionPlan.NameMaxLength);
        RuleFor(r => r.Description).MaximumLength(NutritionPlan.DescriptionMaxLength);
        RuleFor(r => r.EndDate)
            .GreaterThanOrEqualTo(r => r.StartDate)
            .When(r => r.EndDate is not null)
            .WithMessage("A data de fim não pode ser anterior à data de início.");
    }
}

public enum ManageNutritionPlanResult
{
    Done,
    NotFound,
    InvalidState
}

public sealed class ManageNutritionPlanUseCase(
    NutritionistPlanAccess planAccess,
    INutritionPlanRepository planRepository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<ManageNutritionPlanResult> UpdateAsync(
        Guid nutritionistId,
        Guid planId,
        UpdateNutritionPlanRequest request,
        CancellationToken cancellationToken = default)
    {
        var plan = await planAccess.LoadOwnPlanAsync(nutritionistId, planId, cancellationToken);

        if (plan is null)
        {
            return ManageNutritionPlanResult.NotFound;
        }

        if (!plan.IsEditable)
        {
            return ManageNutritionPlanResult.InvalidState;
        }

        plan.UpdateDetails(request.Name, request.Description, request.StartDate, request.EndDate);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ManageNutritionPlanResult.Done;
    }

    public async Task<ManageNutritionPlanResult> ActivateAsync(
        Guid nutritionistId,
        Guid planId,
        CancellationToken cancellationToken = default)
    {
        var plan = await planAccess.LoadOwnPlanAsync(nutritionistId, planId, cancellationToken);

        if (plan is null)
        {
            return ManageNutritionPlanResult.NotFound;
        }

        if (plan.Status != NutritionPlanStatus.Draft)
        {
            return ManageNutritionPlanResult.InvalidState;
        }

        var today = BrazilianCalendar.Today(timeProvider);

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var currentActive = await planRepository.GetActiveForPatientAsync(plan.PatientId, token);

            if (currentActive is not null)
            {
                currentActive.Finish(today);
                await unitOfWork.SaveChangesAsync(token);
            }

            plan.Activate();
            await unitOfWork.SaveChangesAsync(token);
        }, cancellationToken);

        return ManageNutritionPlanResult.Done;
    }

    public async Task<ManageNutritionPlanResult> FinishAsync(
        Guid nutritionistId,
        Guid planId,
        CancellationToken cancellationToken = default)
    {
        var plan = await planAccess.LoadOwnPlanAsync(nutritionistId, planId, cancellationToken);
        if (plan is null)
        {
            return ManageNutritionPlanResult.NotFound;
        }

        if (plan.Status != NutritionPlanStatus.Active)
        {
            return ManageNutritionPlanResult.InvalidState;
        }

        plan.Finish(BrazilianCalendar.Today(timeProvider));
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ManageNutritionPlanResult.Done;
    }

    public async Task<ManageNutritionPlanResult> CancelAsync(
        Guid nutritionistId,
        Guid planId,
        CancellationToken cancellationToken = default)
    {
        var plan = await planAccess.LoadOwnPlanAsync(nutritionistId, planId, cancellationToken);
        if (plan is null)
        {
            return ManageNutritionPlanResult.NotFound;
        }

        if (plan.Status != NutritionPlanStatus.Draft)
        {
            return ManageNutritionPlanResult.InvalidState;
        }

        plan.Cancel();
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ManageNutritionPlanResult.Done;
    }

    public sealed record PlanTargetRequest(
    decimal EnergyKcal,
    decimal? ProteinGrams,
    decimal? CarbohydrateGrams,
    decimal? FatGrams);

    public sealed class PlanTargetRequestValidator : AbstractValidator<PlanTargetRequest>
    {
        public PlanTargetRequestValidator()
        {
            RuleFor(r => r.EnergyKcal).GreaterThan(0).LessThanOrEqualTo(PlanTarget.MaxEnergyKcal);
            RuleFor(r => r.ProteinGrams).InclusiveBetween(0, PlanTarget.MaxMacroGrams).When(r => r.ProteinGrams is not null);
            RuleFor(r => r.CarbohydrateGrams).InclusiveBetween(0, PlanTarget.MaxMacroGrams).When(r => r.CarbohydrateGrams is not null);
            RuleFor(r => r.FatGrams).InclusiveBetween(0, PlanTarget.MaxMacroGrams).When(r => r.FatGrams is not null);
        }
    }

    public async Task<ManageNutritionPlanResult> SetTargetAsync(
    Guid nutritionistId, Guid planId, PlanTargetRequest request, CancellationToken cancellationToken = default)
    {
        var plan = await planAccess.LoadOwnPlanAsync(nutritionistId, planId, cancellationToken);
        if (plan is null)
        {
            return ManageNutritionPlanResult.NotFound;
        }

        if (!plan.IsEditable)
        {
            return ManageNutritionPlanResult.InvalidState;
        }

        plan.SetTarget(new PlanTarget(request.EnergyKcal, request.ProteinGrams, request.CarbohydrateGrams, request.FatGrams));

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ManageNutritionPlanResult.Done;
    }

    public async Task<ManageNutritionPlanResult> ClearTargetAsync(
        Guid nutritionistId, Guid planId, CancellationToken cancellationToken = default)
    {
        var plan = await planAccess.LoadOwnPlanAsync(nutritionistId, planId, cancellationToken);

        if (plan is null)
        {
            return ManageNutritionPlanResult.NotFound;
        }

        if (!plan.IsEditable)
        {
            return ManageNutritionPlanResult.InvalidState;
        }

        plan.ClearTarget();
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ManageNutritionPlanResult.Done;
    }
}
