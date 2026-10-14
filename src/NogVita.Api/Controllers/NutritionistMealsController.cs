using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NogVita.Api.Extensions;
using NogVita.Application.NutritionPlans;
using NogVita.Domain.NutritionPlans;
using NogVita.Domain.Users;

namespace NogVita.Api.Controllers;

[ApiController]
[Route("api/v1/nutritionists/me/nutrition-plans/{planId:guid}/meals")]
[Authorize(Roles = Roles.Nutritionist)]
public sealed class NutritionistMealsController(ManageMealsUseCase manageMealsUseCase) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Add(Guid planId, MealRequest request, CancellationToken cancellationToken)
    {
        var result = await manageMealsUseCase.AddAsync(User.GetUserId(), planId, request, cancellationToken);

        return result.Result == ManageMealResult.Done
            ? Created($"/api/v1/nutritionists/me/nutrition-plans/{planId}", new { id = result.MealId })
            : ToActionResult(result.Result);
    }

    [HttpPut("{mealId:guid}")]
    public async Task<IActionResult> Update(Guid planId, Guid mealId, MealRequest request, CancellationToken cancellationToken) =>
        ToActionResult(await manageMealsUseCase.UpdateAsync(User.GetUserId(), planId, mealId, request, cancellationToken));

    [HttpDelete("{mealId:guid}")]
    public async Task<IActionResult> Remove(Guid planId, Guid mealId, CancellationToken cancellationToken) =>
        ToActionResult(await manageMealsUseCase.RemoveAsync(User.GetUserId(), planId, mealId, cancellationToken));

    [HttpPut("order")]
    public async Task<IActionResult> Reorder(Guid planId, ReorderMealsRequest request, CancellationToken cancellationToken) =>
        ToActionResult(await manageMealsUseCase.ReorderAsync(User.GetUserId(), planId, request, cancellationToken));

    private IActionResult ToActionResult(ManageMealResult result) => result switch
    {
        ManageMealResult.Done => NoContent(),
        ManageMealResult.InvalidState => Problem(
            statusCode: StatusCodes.Status409Conflict,
            title: "Um plano finalizado ou cancelado não pode ser alterado."),
        ManageMealResult.LimitReached => Problem(
            statusCode: StatusCodes.Status409Conflict,
            title: $"O plano já tem o máximo de {NutritionPlan.MaxMeals} refeições."),
        ManageMealResult.InvalidOrder => Problem(
            statusCode: StatusCodes.Status400BadRequest,
            title: "A nova ordem precisa conter todas as refeições do plano, uma vez cada."),
        _ => Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: "Plano ou refeição não encontrado.")
    };
}