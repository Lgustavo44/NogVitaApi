using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NogVita.Api.Extensions;
using NogVita.Api.OpenApi;
using NogVita.Application.Common;
using NogVita.Application.NutritionPlans;
using NogVita.Domain.NutritionPlans;
using NogVita.Domain.Users;

namespace NogVita.Api.Controllers;

/// <summary>Os alimentos de uma refeição.</summary>
/// <remarks>
/// Ao entrar na refeição, o alimento é copiado (os valores por 100 g e a data da cópia): uma correção posterior na base
/// não muda o que o paciente recebeu. O mesmo alimento entra uma vez por refeição, e uma refeição tem no máximo 20 alimentos.
/// </remarks>
[ApiController]
[Route("api/v1/nutritionists/me/nutrition-plans/{planId:guid}/meals/{mealId:guid}/foods")]
[Authorize(Roles = Roles.Nutritionist)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
[ProducesProblem(StatusCodes.Status500InternalServerError)]
public sealed class NutritionistMealFoodsController(ManageMealFoodsUseCase manageMealFoodsUseCase) : ControllerBase
{
    /// <summary>Acrescenta um alimento à refeição, com a quantidade em gramas.</summary>
    /// <param name="planId">O Id do plano.</param>
    /// <param name="mealId">O Id da refeição.</param>
    [HttpPost]
    [ProducesResponseType<IdResponse>(StatusCodes.Status201Created)]
    [ProducesValidationProblem]
    [ProducesProblem(StatusCodes.Status404NotFound)]
    [ProducesProblem(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Add(Guid planId, Guid mealId, AddMealFoodRequest request, CancellationToken cancellationToken)
    {
        var result = await manageMealFoodsUseCase.AddAsync(User.GetUserId(), planId, mealId, request, cancellationToken);

        return result.Result == ManageMealFoodResult.Done
            ? Created($"/api/v1/nutritionists/me/nutrition-plans/{planId}", new IdResponse(result.MealFoodId!.Value))
            : ToActionResult(result.Result);
    }

    /// <summary>Altera a quantidade de um alimento da refeição. A cópia dos valores não muda.</summary>
    /// <param name="planId">O Id do plano.</param>
    /// <param name="mealId">O Id da refeição.</param>
    /// <param name="mealFoodId">O Id do item da refeição (e não o do alimento).</param>
    [HttpPut("{mealFoodId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesValidationProblem]
    [ProducesProblem(StatusCodes.Status404NotFound)]
    [ProducesProblem(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateQuantity(Guid planId, Guid mealId, Guid mealFoodId, UpdateMealFoodRequest request, CancellationToken cancellationToken)
    {
        var result = await manageMealFoodsUseCase.UpdateQuantityAsync(User.GetUserId(), planId, mealId, mealFoodId, request, cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>Remove um alimento da refeição.</summary>
    /// <param name="planId">O Id do plano.</param>
    /// <param name="mealId">O Id da refeição.</param>
    /// <param name="mealFoodId">O Id do item da refeição (e não o do alimento).</param>
    [HttpDelete("{mealFoodId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesProblem(StatusCodes.Status404NotFound)]
    [ProducesProblem(StatusCodes.Status409Conflict)]
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
        ManageMealFoodResult.NotFound => Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: "Plano, refeição ou alimento da refeição não encontrado.")
    };
}
