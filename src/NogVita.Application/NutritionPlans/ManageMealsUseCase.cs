using FluentValidation;
using NogVita.Application.Abstractions;
using NogVita.Domain.NutritionPlans;

namespace NogVita.Application.NutritionPlans;

// SuggestedTime é opcional, no formato "HH:mm". Qualquer horário é aceito: o front avisa quando ele cai fora da rotina do paciente.
public sealed record MealRequest(string Name, string? Notes, TimeOnly? SuggestedTime = null);

public sealed class MealRequestValidator : AbstractValidator<MealRequest>
{
    public MealRequestValidator()
    {
        RuleFor(r => r.Name).NotEmpty().MaximumLength(Meal.NameMaxLength);
        RuleFor(r => r.Notes).MaximumLength(Meal.NotesMaxLength);
    }
}

public sealed record ReorderMealsRequest(IReadOnlyList<Guid> MealIds);

public sealed class ReorderMealsRequestValidator : AbstractValidator<ReorderMealsRequest>
{
    public ReorderMealsRequestValidator()
    {
        RuleFor(r => r.MealIds).NotEmpty();
    }
}

public enum ManageMealResult
{
    Done,
    NotFound,
    InvalidState,
    LimitReached,
    InvalidOrder
}

public sealed record AddMealResult(ManageMealResult Result, Guid? MealId = null);

public sealed class ManageMealsUseCase(NutritionistPlanAccess planAccess, IUnitOfWork unitOfWork)
{
    public async Task<AddMealResult> AddAsync(
        Guid nutritionistId, Guid planId, MealRequest request, CancellationToken cancellationToken = default)
    {
        var plan = await planAccess.LoadOwnPlanAsync(nutritionistId, planId, cancellationToken);

        if (plan is null)
        {
            return new AddMealResult(ManageMealResult.NotFound);
        }

        if (!plan.IsEditable)
        {
            return new AddMealResult(ManageMealResult.InvalidState);
        }

        if (plan.Meals.Count >= NutritionPlan.MaxMeals)
        {
            return new AddMealResult(ManageMealResult.LimitReached);
        }

        var meal = plan.AddMeal(request.Name, request.Notes, request.SuggestedTime);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new AddMealResult(ManageMealResult.Done, meal.Id);
    }

    public async Task<ManageMealResult> UpdateAsync(
        Guid nutritionistId, Guid planId, Guid mealId, MealRequest request, CancellationToken cancellationToken = default)
    {
        var plan = await planAccess.LoadOwnPlanAsync(nutritionistId, planId, cancellationToken);
        
        if (plan is null)
        {
            return ManageMealResult.NotFound;
        }

        if (!plan.IsEditable)
        {
            return ManageMealResult.InvalidState;
        }

        if (!plan.Meals.Any(m => m.Id == mealId))
        {
            return ManageMealResult.NotFound;
        }

        plan.UpdateMeal(mealId, request.Name, request.Notes, request.SuggestedTime);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ManageMealResult.Done;
    }

    public async Task<ManageMealResult> RemoveAsync(
        Guid nutritionistId, Guid planId, Guid mealId, CancellationToken cancellationToken = default)
    {
        var plan = await planAccess.LoadOwnPlanAsync(nutritionistId, planId, cancellationToken);

        if (plan is null)
        {
            return ManageMealResult.NotFound;
        }

        if (!plan.IsEditable)
        {
            return ManageMealResult.InvalidState;
        }

        if (!plan.Meals.Any(m => m.Id == mealId))
        {
            return ManageMealResult.NotFound;
        }

        plan.RemoveMeal(mealId);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ManageMealResult.Done;
    }

    public async Task<ManageMealResult> ReorderAsync(
        Guid nutritionistId, Guid planId, ReorderMealsRequest request, CancellationToken cancellationToken = default)
    {
        var plan = await planAccess.LoadOwnPlanAsync(nutritionistId, planId, cancellationToken);

        if (plan is null)
        {
            return ManageMealResult.NotFound;
        }

        if (!plan.IsEditable)
        {
            return ManageMealResult.InvalidState;
        }

        if (!plan.IsCompleteMealOrder(request.MealIds))
        {
            return ManageMealResult.InvalidOrder;
        }

        plan.ReorderMeals(request.MealIds);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ManageMealResult.Done;
    }
}