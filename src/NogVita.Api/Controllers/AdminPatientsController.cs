using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NogVita.Application.Admin;
using NogVita.Domain.Users;

namespace NogVita.Api.Controllers;

[ApiController]
[Route("api/v1/admin/patients")]
[Authorize(Roles = Roles.Admin)]
public sealed class AdminPatientsController(IAdminUserQueries adminUserQueries) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] AdminUserListRequest request, CancellationToken cancellationToken)
    {
        var response = await adminUserQueries.ListPatientsAsync(request, cancellationToken);

        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var user = await adminUserQueries.GetUserAsync(id, cancellationToken);

        if (user?.Patient is null)
            return Problem(statusCode: StatusCodes.Status404NotFound, title: "Paciente não encontrado.");

        return Ok(user);
    }
}