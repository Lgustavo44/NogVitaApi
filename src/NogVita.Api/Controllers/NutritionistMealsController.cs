using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NogVita.Api.Extensions;
using NogVita.Api.OpenApi;
using NogVita.Application.Common;
using NogVita.Application.NutritionPlans;
using NogVita.Domain.NutritionPlans;
using NogVita.Domain.Users;

namespace NogVita.Api.Controllers;

/// <summary>As refeições de um plano.</summary>
/// <remarks>
/// Só em planos `Draft` ou `Active`. A ordem é sempre sequencial (1, 2, 3...), e um plano tem no máximo 10 refeições.
/// Para ver as refeições, use o detalhe do plano.
/// </remarks>
[ApiController]
[Route("api/v1/nutritionists/me/nutrition-plans/{planId:guid}/meals")]
[Authorize(Roles = Roles.Nutritionist)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
[ProducesProblem(StatusCodes.Status500InternalServerError)]
public sealed class NutritionistMealsController(ManageMealsUseCase manageMealsUseCase) : ControllerBase
{
    /// <summary>Acrescenta uma refeição no fim da ordem.</summary>
    /// <remarks>O horário sugerido (`"HH:mm"`) é opcional e não muda a ordem.</remarks>
    /// <param name="planId">O Id do plano.</param>
    [HttpPost]
    [ProducesResponseType<IdResponse>(StatusCodes.Status201Created)]
    [ProducesValidationProblem]
    [ProducesProblem(StatusCodes.Status404NotFound)]
    [ProducesProblem(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Add(Guid planId, MealRequest request, CancellationToken cancellationToken)
    {
        var result = await manageMealsUseCase.AddAsync(User.GetUserId(), planId, request, cancellationToken);

        return result.Result == ManageMealResult.Done
            ? Created($"/api/v1/nutritionists/me/nutrition-plans/{planId}", new IdResponse(result.MealId!.Value))
            : ToActionResult(result.Result);
    }

    /// <summary>Altera o nome, as observações e o horário de uma refeição, sem mudar a ordem.</summary>
    /// <param name="planId">O Id do plano.</param>
    /// <param name="mealId">O Id da refeição.</param>
    [HttpPut("{mealId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesValidationProblem]
    [ProducesProblem(StatusCodes.Status404NotFound)]
    [ProducesProblem(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(Guid planId, Guid mealId, MealRequest request, CancellationToken cancellationToken) =>
        ToActionResult(await manageMealsUseCase.UpdateAsync(User.GetUserId(), planId, mealId, request, cancellationToken));

    /// <summary>Remove uma refeição e renumera as outras, sem deixar buracos.</summary>
    /// <param name="planId">O Id do plano.</param>
    /// <param name="mealId">O Id da refeição.</param>
    [HttpDelete("{mealId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesProblem(StatusCodes.Status404NotFound)]
    [ProducesProblem(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Remove(Guid planId, Guid mealId, CancellationToken cancellationToken) =>
        ToActionResult(await manageMealsUseCase.RemoveAsync(User.GetUserId(), planId, mealId, cancellationToken));

    /// <summary>Define a nova ordem das refeições.</summary>
    /// <remarks>A lista precisa ter todas as refeições do plano, cada uma uma vez, na ordem desejada; senão, 400 e nada muda.</remarks>
    /// <param name="planId">O Id do plano.</param>
    [HttpPut("order")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesValidationProblem]
    [ProducesProblem(StatusCodes.Status400BadRequest)]
    [ProducesProblem(StatusCodes.Status404NotFound)]
    [ProducesProblem(StatusCodes.Status409Conflict)]
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
        ManageMealResult.NotFound => Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: "Plano ou refeição não encontrado.")
    };
}
