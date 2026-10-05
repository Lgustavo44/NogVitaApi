using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NogVita.Api.Extensions;
using NogVita.Application.Admin;
using NogVita.Domain.Users;

namespace NogVita.Api.Controllers;

[ApiController]
[Route("api/v1/admin/users")]
[Authorize(Roles = Roles.Admin)]
public sealed class AdminUsersController(IAdminUserQueries adminUserQueries, DeactivateUserUseCase deactivateUserUseCase, ActivateUserUseCase activateUserUseCase) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] AdminUserListRequest request, CancellationToken cancellationToken)
    {
        var response = await adminUserQueries.ListUsersAsync(request, cancellationToken);

        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var user = await adminUserQueries.GetUserAsync(id, cancellationToken);

        if (user is null)
            return Problem(statusCode: StatusCodes.Status404NotFound, title: "Usuário não encontrado.");

        return Ok(user);
    }

    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        var result = await deactivateUserUseCase.ExecuteAsync(User.GetUserId(), id, cancellationToken);

        return result switch
        {
            DeactivateUserResult.Success => NoContent(),
            DeactivateUserResult.NotFound => Problem(statusCode: StatusCodes.Status404NotFound, title: "Usuário não encontrado."),
            DeactivateUserResult.CannotDeactivateSelf => Problem(statusCode: StatusCodes.Status409Conflict, title: "Você não pode desativar a sua própria conta."),
            _ => throw new InvalidOperationException($"Resultado não tratado: {result}")
        };
    }
    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        var result = await activateUserUseCase.ExecuteAsync(User.GetUserId(), cancellationToken);

        return result switch
        {
            ActivateUserResult.Success => NoContent(),
            ActivateUserResult.NotFound => Problem(statusCode: StatusCodes.Status404NotFound, title: "Usuário não encontrado."),
            ActivateUserResult.PasswordNotDefined => Problem(statusCode: StatusCodes.Status400BadRequest, title: "Senha não definida."),
            _ => throw new InvalidOperationException($"Resultado não tratado: {result}")
        };
    }
}