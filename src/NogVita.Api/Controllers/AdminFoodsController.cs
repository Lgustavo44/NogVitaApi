using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NogVita.Api.Extensions;
using NogVita.Api.OpenApi;
using NogVita.Application.Common.Pagination;
using NogVita.Application.Foods;
using NogVita.Domain.Users;

namespace NogVita.Api.Controllers;

/// <summary>A gestão da base de alimentos pelo administrador.</summary>
[ApiController]
[Route("api/v1/admin/foods")]
[Authorize(Roles = Roles.Admin)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
[ProducesProblem(StatusCodes.Status500InternalServerError)]
public sealed class AdminFoodsController(
    SetFoodActiveUseCase setFoodActiveUseCase,
    IFoodQueries foodQueries) : ControllerBase
{
    /// <summary>Busca alimentos, incluindo os desativados (`isActive: false`).</summary>
    /// <remarks>
    /// A mesma busca de `GET /api/v1/foods`, para encontrar o que reativar. É uma rota separada de propósito:
    /// não existe parâmetro que faça um nutricionista ver alimentos desativados.
    /// </remarks>
    [HttpGet]
    [ProducesResponseType<PagedResponse<FoodResponse>>(StatusCodes.Status200OK)]
    [ProducesValidationProblem]
    public async Task<IActionResult> Search([FromQuery] FoodSearchRequest request, CancellationToken cancellationToken) =>
        Ok(await foodQueries.SearchAsync(request, includeInactive: true, cancellationToken));

    /// <summary>Reativa um alimento.</summary>
    /// <param name="id">O Id do alimento.</param>
    [HttpPost("{id:guid}/activate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesProblem(StatusCodes.Status404NotFound)]
    public Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken) =>
        SetActive(id, active: true, cancellationToken);

    /// <summary>Desativa um alimento com dados errados.</summary>
    /// <remarks>
    /// Ele some da busca e da prévia, e não volta por uma nova importação. Os planos que já usam o alimento não mudam:
    /// eles guardam uma cópia dos valores.
    /// </remarks>
    /// <param name="id">O Id do alimento.</param>
    [HttpPost("{id:guid}/deactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesProblem(StatusCodes.Status404NotFound)]
    public Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken) =>
        SetActive(id, active: false, cancellationToken);

    private async Task<IActionResult> SetActive(Guid id, bool active, CancellationToken cancellationToken)
    {
        var found = await setFoodActiveUseCase.ExecuteAsync(User.GetUserId(), id, active, cancellationToken);

        return found
            ? NoContent()
            : Problem(statusCode: StatusCodes.Status404NotFound, title: "Alimento não encontrado.");
    }
}
