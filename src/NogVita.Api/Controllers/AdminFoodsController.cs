using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NogVita.Api.Extensions;
using NogVita.Application.Foods;
using NogVita.Domain.Users;

namespace NogVita.Api.Controllers;

[ApiController]
[Route("api/v1/admin/foods")]
[Authorize(Roles = Roles.Admin)]
public sealed class AdminFoodsController(
    SetFoodActiveUseCase setFoodActiveUseCase,
    IFoodQueries foodQueries) : ControllerBase
{
    // Endpoint separado, e não um ?includeInactive=true na busca comum: o nutricionista nunca vê os inativos.
    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] FoodSearchRequest request, CancellationToken cancellationToken) =>
        Ok(await foodQueries.SearchAsync(request, includeInactive: true, cancellationToken));

    [HttpPost("{id:guid}/activate")]
    public Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken) =>
        SetActive(id, active: true, cancellationToken);

    [HttpPost("{id:guid}/deactivate")]
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