using FluentValidation;
using NogVita.Application.Abstractions;
using NogVita.Domain.NutritionPlans;

namespace NogVita.Application.NutritionPlans;

public sealed record AddMealFoodRequest(Guid FoodId, decimal QuantityInGrams);

public sealed class AddMealFoodRequestValidator : AbstractValidator<AddMealFoodRequest>
{
    public AddMealFoodRequestValidator()
    {
        RuleFor(r => r.FoodId).NotEmpty();
        RuleFor(r => r.QuantityInGrams)
            .GreaterThan(0)
            .LessThanOrEqualTo(MealFood.MaxQuantityInGrams);
    }
}

public sealed record UpdateMealFoodRequest(decimal QuantityInGrams);

public sealed class UpdateMealFoodRequestValidator : AbstractValidator<UpdateMealFoodRequest>
{
    public UpdateMealFoodRequestValidator()
    {
        RuleFor(r => r.QuantityInGrams)
            .GreaterThan(0)
            .LessThanOrEqualTo(MealFood.MaxQuantityInGrams);
    }
}

public enum ManageMealFoodResult
{
    Done,
    NotFound,
    InvalidState,
    FoodNotFound,
    AlreadyInMeal,
    LimitReached
}

public sealed record AddMealFoodResult(ManageMealFoodResult Result, Guid? MealFoodId = null);

public sealed class ManageMealFoodsUseCase(
    NutritionistPlanAccess planAccess,
    IFoodRepository foodRepository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<AddMealFoodResult> AddAsync(
        Guid nutritionistId,
        Guid planId,
        Guid mealId,
        AddMealFoodRequest request,
        CancellationToken cancellationToken = default)
    {
        var plan = await planAccess.LoadOwnPlanAsync(nutritionistId, planId, cancellationToken);

        if (plan is null)
        {
            return new AddMealFoodResult(ManageMealFoodResult.NotFound);
        }

        if (!plan.IsEditable)
        {
            return new AddMealFoodResult(ManageMealFoodResult.InvalidState);
        }

        var meal = plan.Meals.FirstOrDefault(m => m.Id == mealId);

        if (meal is null)
        {
            return new AddMealFoodResult(ManageMealFoodResult.NotFound);
        }

        var food = await foodRepository.GetByIdAsync(request.FoodId, cancellationToken);

        if (food is null || !food.IsActive)
        {
            return new AddMealFoodResult(ManageMealFoodResult.FoodNotFound);
        }

        if (meal.Foods.Any(f => f.FoodId == food.Id))
        {
            return new AddMealFoodResult(ManageMealFoodResult.AlreadyInMeal);
        }

        if (meal.Foods.Count >= Meal.MaxFoods)
        {
            return new AddMealFoodResult(ManageMealFoodResult.LimitReached);
        }

        var mealFood = plan.AddFoodToMeal(mealId, food, request.QuantityInGrams, timeProvider.GetUtcNow().UtcDateTime);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new AddMealFoodResult(ManageMealFoodResult.Done, mealFood.Id);
    }

    public async Task<ManageMealFoodResult> UpdateQuantityAsync(
        Guid nutritionistId,
        Guid planId,
        Guid mealId,
        Guid mealFoodId,
        UpdateMealFoodRequest request,
        CancellationToken cancellationToken = default)
    {
        var plan = await LoadEditablePlanWithItemAsync(nutritionistId, planId, mealId, mealFoodId, cancellationToken);

        if (plan.Result != ManageMealFoodResult.Done)
        {
            return plan.Result;
        }

        plan.Plan!.UpdateMealFoodQuantity(mealId, mealFoodId, request.QuantityInGrams);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ManageMealFoodResult.Done;
    }

    public async Task<ManageMealFoodResult> RemoveAsync(
        Guid nutritionistId,
        Guid planId,
        Guid mealId,
        Guid mealFoodId,
        CancellationToken cancellationToken = default)
    {
        var plan = await LoadEditablePlanWithItemAsync(nutritionistId, planId, mealId, mealFoodId, cancellationToken);

        if (plan.Result != ManageMealFoodResult.Done)
        {
            return plan.Result;
        }

        plan.Plan!.RemoveFoodFromMeal(mealId, mealFoodId);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ManageMealFoodResult.Done;
    }

    private async Task<(NutritionPlan? Plan, ManageMealFoodResult Result)> LoadEditablePlanWithItemAsync(
        Guid nutritionistId,
        Guid planId,
        Guid mealId,
        Guid mealFoodId,
        CancellationToken cancellationToken)
    {
        var plan = await planAccess.LoadOwnPlanAsync(nutritionistId, planId, cancellationToken);

        if (plan is null)
        {
            return (null, ManageMealFoodResult.NotFound);
        }

        if (!plan.IsEditable)
        {
            return (null, ManageMealFoodResult.InvalidState);
        }

        var itemExists = plan.Meals
            .FirstOrDefault(m => m.Id == mealId)?
            .Foods.Any(f => f.Id == mealFoodId) ?? false;

        return itemExists
            ? (plan, ManageMealFoodResult.Done)
            : (null, ManageMealFoodResult.NotFound);
    }
}