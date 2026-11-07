using FluentValidation;
using NogVita.Application.Abstractions;
using NogVita.Application.Foods;
using NogVita.Domain.Foods;
using NogVita.Domain.NutritionPlans;

namespace NogVita.Application.NutritionPlans;

public sealed record AddMealFoodRequest(
    Guid FoodId,
    decimal? QuantityInGrams,
    Guid? PortionId = null,
    decimal? PortionQuantity = null,
    Guid? SubstituteForId = null) : IFoodAmountRequest;

public sealed class AddMealFoodRequestValidator : AbstractValidator<AddMealFoodRequest>
{
    public AddMealFoodRequestValidator()
    {
        RuleFor(r => r.FoodId).NotEmpty().WithMessage("O alimento é obrigatório.");
        this.AddFoodAmountRules();
    }
}

public sealed record UpdateMealFoodRequest(decimal? QuantityInGrams, Guid? PortionId = null, decimal? PortionQuantity = null) : IFoodAmountRequest;

public sealed class UpdateMealFoodRequestValidator : AbstractValidator<UpdateMealFoodRequest>
{
    public UpdateMealFoodRequestValidator() => this.AddFoodAmountRules();
}

public enum ManageMealFoodResult
{
    Done,
    NotFound,
    InvalidState,
    FoodNotFound,
    AlreadyInMeal,
    LimitReached,
    PortionNotFound
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

        // A alternativa precisa apontar para um item desta refeição (as outras regras ficam no domínio, com 422).
        if (request.SubstituteForId is { } mainId && meal.Foods.All(f => f.Id != mainId))
        {
            return new AddMealFoodResult(ManageMealFoodResult.NotFound);
        }

        var quantity = FoodAmount.Resolve(food, request);

        if (quantity is null)
        {
            return new AddMealFoodResult(ManageMealFoodResult.PortionNotFound);
        }

        var mealFood = plan.AddFoodToMeal(mealId, food, quantity, timeProvider.GetUtcNow().UtcDateTime, request.SubstituteForId);
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

        // A porção é do alimento do item: ela só é buscada quando o pedido usa uma.
        var quantity = request.PortionId is null
            ? FoodQuantity.InGrams(request.QuantityInGrams!.Value)
            : await ResolvePortionAsync(plan.Plan!, mealId, mealFoodId, request, cancellationToken);

        if (quantity is null)
        {
            return ManageMealFoodResult.PortionNotFound;
        }

        plan.Plan!.UpdateMealFoodQuantity(mealId, mealFoodId, quantity);
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

    private async Task<FoodQuantity?> ResolvePortionAsync(
        NutritionPlan plan, Guid mealId, Guid mealFoodId, UpdateMealFoodRequest request, CancellationToken cancellationToken)
    {
        var foodId = plan.Meals.First(m => m.Id == mealId).Foods.First(f => f.Id == mealFoodId).FoodId;
        var food = await foodRepository.GetByIdAsync(foodId, cancellationToken);

        return food is null ? null : FoodAmount.Resolve(food, request);
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
