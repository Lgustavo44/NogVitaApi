using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NogVita.Api.Extensions;
using NogVita.Application.Nutritionists;
using NogVita.Domain.Users;

namespace NogVita.Api.Controllers;

[ApiController]
[Route("api/v1/nutritionists")]
[Authorize]
public sealed class NutritionistsController(
    INutritionistQueries nutritionistQueries,
    GetMyNutritionistProfileUseCase getMyProfileUseCase,
    UpdateMyNutritionistProfileUseCase updateMyProfileUseCase) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] NutritionistListRequest request, CancellationToken cancellationToken)
    {
        var response = await nutritionistQueries.ListAsync(request, cancellationToken);

        return Ok(response);
    }

    [HttpGet("me")]
    [Authorize(Roles = Roles.Nutritionist)]
    public async Task<IActionResult> GetMe(CancellationToken cancellationToken)
    {
        var response = await getMyProfileUseCase.ExecuteAsync(User.GetUserId(), cancellationToken);

        return response is null ? ProfileNotFound() : Ok(response);
    }

    [HttpPut("me")]
    [Authorize(Roles = Roles.Nutritionist)]
    public async Task<IActionResult> UpdateMe(UpdateMyNutritionistProfileRequest request, CancellationToken cancellationToken)
    {
        var response = await updateMyProfileUseCase.ExecuteAsync(User.GetUserId(), request, cancellationToken);

        return response is null ? ProfileNotFound() : Ok(response);
    }

    private ObjectResult ProfileNotFound() =>
        Problem(statusCode: StatusCodes.Status404NotFound, title: "Perfil de nutricionista não encontrado.");
}