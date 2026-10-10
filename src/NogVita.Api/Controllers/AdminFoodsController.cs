using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NogVita.Application.Foods;
using NogVita.Domain.Users;

namespace NogVita.Api.Controllers;

[ApiController]
[Route("api/v1/admin/foods")]
[Authorize(Roles = Roles.Admin)]
public sealed class AdminFoodsController(SetFoodActiveUseCase setFoodActiveUseCase) : ControllerBase
{
    [HttpPost("{id:guid}/activate")]
    public Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken) =>
        SetActive(id, active: true, cancellationToken);

    [HttpPost("{id:guid}/deactivate")]
    public Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken) =>
        SetActive(id, active: false, cancellationToken);

    private async Task<IActionResult> SetActive(Guid id, bool active, CancellationToken cancellationToken)
    {
        var found = await setFoodActiveUseCase.ExecuteAsync(id, active, cancellationToken);

        return found
            ? NoContent()
            : Problem(statusCode: StatusCodes.Status404NotFound, title: "Alimento não encontrado.");
    }
}