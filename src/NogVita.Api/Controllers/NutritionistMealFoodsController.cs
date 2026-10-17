using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NogVita.Api.Extensions;
using NogVita.Application.NutritionPlans;
using NogVita.Domain.NutritionPlans;
using NogVita.Domain.Users;

namespace NogVita.Api.Controllers;

[ApiController]
[Route("api/v1/nutritionists/me/nutrition-plans/{planId:guid}/meals/{mealId:guid}/foods")]
[Authorize(Roles = Roles.Nutritionist)]
public sealed class NutritionistMealFoodsController(ManageMealFoodsUseCase manageMealFoodsUseCase) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Add(Guid planId, Guid mealId, AddMealFoodRequest request, CancellationToken cancellationToken)
    {
        var result = await manageMealFoodsUseCase.AddAsync(User.GetUserId(), planId, mealId, request, cancellationToken);

        return result.Result == ManageMealFoodResult.Done
            ? Created($"/api/v1/nutritionists/me/nutrition-plans/{planId}", new { id = result.MealFoodId })
            : ToActionResult(result.Result);
    }

    [HttpPut("{mealFoodId:guid}")]
    public async Task<IActionResult> UpdateQuantity(Guid planId, Guid mealId, Guid mealFoodId, UpdateMealFoodRequest request, CancellationToken cancellationToken)
    {
        var result = await manageMealFoodsUseCase.UpdateQuantityAsync(User.GetUserId(), planId, mealId, mealFoodId, request, cancellationToken);
        return ToActionResult(result);
    }

    [HttpDelete("{mealFoodId:guid}")]
    public async Task<IActionResult> Remove(Guid planId, Guid mealId, Guid mealFoodId, CancellationToken cancellationToken)
    {
        var result = await manageMealFoodsUseCase.RemoveAsync(User.GetUserId(), planId, mealId, mealFoodId, cancellationToken);
        return ToActionResult(result);
    }

    private IActionResult ToActionResult(ManageMealFoodResult result) => result switch
    {
        ManageMealFoodResult.Done => NoContent(),
        ManageMealFoodResult.InvalidState => Problem(
            statusCode: StatusCodes.Status409Conflict,
            title: "Um plano finalizado ou cancelado não pode ser alterado."),
        ManageMealFoodResult.FoodNotFound => Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: "Alimento não encontrado ou indisponível."),
        ManageMealFoodResult.AlreadyInMeal => Problem(
            statusCode: StatusCodes.Status409Conflict,
            title: "Este alimento já está na refeição. Altere a quantidade dele."),
        ManageMealFoodResult.LimitReached => Problem(
            statusCode: StatusCodes.Status409Conflict,
            title: $"Uma refeição pode ter no máximo {Meal.MaxFoods} alimentos."),
        _ => Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: "Plano, refeição ou alimento da refeição não encontrado.")
    };
}