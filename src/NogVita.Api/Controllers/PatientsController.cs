using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NogVita.Api.Extensions;
using NogVita.Application.Patients;
using NogVita.Domain.Users;

namespace NogVita.Api.Controllers;

[ApiController]
[Route("api/v1/patients")]
[Authorize(Roles = Roles.Patient)]
public sealed class PatientsController(
    GetMyPatientProfileUseCase getMyPatientProfileUseCase,
    UpdateMyPatientProfileUseCase updateMyPatientProfileUseCase) : ControllerBase
{
    [HttpGet("me")]
    public async Task<IActionResult> GetMe(CancellationToken cancellationToken)
    {
        var response = await getMyPatientProfileUseCase.ExecuteAsync(User.GetUserId(), cancellationToken);

        if (response is null)
            return Problem(statusCode: StatusCodes.Status404NotFound, title: "Perfil de paciente não encontrado.");

        return Ok(response);
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateMe(UpdatePatientProfileRequest request, CancellationToken cancellationToken)
    {
        var response = await updateMyPatientProfileUseCase.ExecuteAsync(User.GetUserId(), request, cancellationToken);

        if (response is null)
            return Problem(statusCode: StatusCodes.Status404NotFound, title: "Perfil de paciente não encontrado.");

        return Ok(response);
    }
}