using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NogVita.Application.Admin;
using NogVita.Domain.Users;

namespace NogVita.Api.Controllers;

[ApiController]
[Route("api/v1/admin/nutritionists")]
[Authorize(Roles = Roles.Admin)]
public sealed class AdminNutritionistsController(IAdminUserQueries adminUserQueries) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] AdminUserListRequest request, CancellationToken cancellationToken)
    {
        var response = await adminUserQueries.ListNutritionistsAsync(request, cancellationToken);

        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var user = await adminUserQueries.GetUserAsync(id, cancellationToken);

        if (user?.Nutritionist is null)
            return Problem(statusCode: StatusCodes.Status404NotFound, title: "Nutricionista não encontrado.");

        return Ok(user);
    }
}