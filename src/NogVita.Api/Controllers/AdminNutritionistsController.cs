using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NogVita.Application.Admin;
using NogVita.Application.Nutritionists;
using NogVita.Domain.Users;

namespace NogVita.Api.Controllers;

[ApiController]
[Route("api/v1/admin/nutritionists")]
[Authorize(Roles = Roles.Admin)]
public sealed class AdminNutritionistsController(
    IAdminUserQueries adminUserQueries,
    PreRegisterNutritionistUseCase preRegisterNutritionistUseCase,
    ResendNutritionistInvitationUseCase resendNutritionistInvitationUseCase) : ControllerBase
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

    [HttpPost]
    public async Task<IActionResult> PreRegister(PreRegisterNutritionistRequest request, CancellationToken cancellationToken)
    {
        var result = await preRegisterNutritionistUseCase.ExecuteAsync(request, cancellationToken);

        if (result.Status == PreRegisterNutritionistStatus.Conflict)
            return Problem(statusCode: StatusCodes.Status409Conflict, title: "Não foi possível pré-cadastrar o nutricionista com esses dados.");

        var response = new PreRegisterNutritionistResponse(result.UserId!.Value, result.InvitationEmailSent);

        return Created($"/api/v1/admin/nutritionists/{response.UserId}", response);
    }

    [HttpPost("{id:guid}/invitation")]
    public async Task<IActionResult> ResendInvitation(Guid id, CancellationToken cancellationToken)
    {
        var status = await resendNutritionistInvitationUseCase.ExecuteAsync(id, cancellationToken);

        return status switch
        {
            ResendInvitationStatus.Sent => Ok(new { invitationEmailSent = true }),
            ResendInvitationStatus.EmailFailed => Ok(new { invitationEmailSent = false }),
            ResendInvitationStatus.NotFound => Problem(statusCode: StatusCodes.Status404NotFound, title: "Nutricionista não encontrado."),
            ResendInvitationStatus.AlreadyActive => Problem(statusCode: StatusCodes.Status409Conflict, title: "O nutricionista já aceitou o convite."),
            _ => throw new InvalidOperationException($"Resultado não tratado: {status}")
        };
    }
}