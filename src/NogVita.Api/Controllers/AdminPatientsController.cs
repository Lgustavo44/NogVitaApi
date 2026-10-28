using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NogVita.Api.OpenApi;
using NogVita.Application.Admin;
using NogVita.Application.Common.Pagination;
using NogVita.Domain.Users;

namespace NogVita.Api.Controllers;

/// <summary>Os pacientes, pelo administrador.</summary>
[ApiController]
[Route("api/v1/admin/patients")]
[Authorize(Roles = Roles.Admin)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
[ProducesProblem(StatusCodes.Status500InternalServerError)]
public sealed class AdminPatientsController(IAdminUserQueries adminUserQueries) : ControllerBase
{
    /// <summary>Lista os pacientes, do mais novo para o mais antigo.</summary>
    /// <remarks>`search` busca no nome ou no e-mail; `isActive` filtra pela situação da conta.</remarks>
    [HttpGet]
    [ProducesResponseType<PagedResponse<AdminPatientSummary>>(StatusCodes.Status200OK)]
    [ProducesValidationProblem]
    public async Task<IActionResult> List([FromQuery] AdminUserListRequest request, CancellationToken cancellationToken)
    {
        var response = await adminUserQueries.ListPatientsAsync(request, cancellationToken);

        return Ok(response);
    }

    /// <summary>Devolve os detalhes de um paciente.</summary>
    /// <param name="id">O Id do usuário.</param>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<AdminUserDetail>(StatusCodes.Status200OK)]
    [ProducesProblem(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var user = await adminUserQueries.GetUserAsync(id, cancellationToken);

        if (user?.Patient is null)
            return Problem(statusCode: StatusCodes.Status404NotFound, title: "Paciente não encontrado.");

        return Ok(user);
    }
}
