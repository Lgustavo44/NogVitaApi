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
    CareAccessService careAccessService,
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
        var plan = await LoadOwnPlanAsync(nutritionistId, planId, cancellationToken);

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
        var plan = await LoadOwnPlanAsync(nutritionistId, planId, cancellationToken);

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
        var plan = await LoadOwnPlanAsync(nutritionistId, planId, cancellationToken);
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
        var plan = await LoadOwnPlanAsync(nutritionistId, planId, cancellationToken);
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

    private async Task<NutritionPlan?> LoadOwnPlanAsync(
        Guid nutritionistId,
        Guid planId,
        CancellationToken cancellationToken)
    {
        var plan = await planRepository.GetByIdAsync(planId, cancellationToken);

        if (plan is null || plan.NutritionistId != nutritionistId)
        {
            return null;
        }

        var followsPatient = await careAccessService.NutritionistFollowsPatientAsync(
            nutritionistId, plan.PatientId, cancellationToken);

        return followsPatient ? plan : null;
    }
}