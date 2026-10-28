using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NogVita.Api.Extensions;
using NogVita.Api.OpenApi;
using NogVita.Application.Patients;
using NogVita.Domain.Users;

namespace NogVita.Api.Controllers;

/// <summary>O perfil do paciente conectado.</summary>
[ApiController]
[Route("api/v1/patients")]
[Authorize(Roles = Roles.Patient)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
[ProducesProblem(StatusCodes.Status500InternalServerError)]
public sealed class PatientsController(
    GetMyPatientProfileUseCase getMyPatientProfileUseCase,
    UpdateMyPatientProfileUseCase updateMyPatientProfileUseCase) : ControllerBase
{
    /// <summary>Devolve o perfil do paciente: dados de saúde, objetivo e rotina.</summary>
    [HttpGet("me")]
    [ProducesResponseType<PatientProfileResponse>(StatusCodes.Status200OK)]
    [ProducesProblem(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMe(CancellationToken cancellationToken)
    {
        var response = await getMyPatientProfileUseCase.ExecuteAsync(User.GetUserId(), cancellationToken);

        if (response is null)
            return Problem(statusCode: StatusCodes.Status404NotFound, title: "Perfil de paciente não encontrado.");

        return Ok(response);
    }

    /// <summary>Atualiza o perfil do paciente, inclusive a rotina.</summary>
    /// <remarks>
    /// Envie sempre os três horários da rotina (acordar, dormir e treinar): um horário omitido é apagado.
    /// O paciente precisa ter pelo menos 18 anos.
    /// </remarks>
    [HttpPut("me")]
    [ProducesResponseType<PatientProfileResponse>(StatusCodes.Status200OK)]
    [ProducesValidationProblem]
    [ProducesProblem(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateMe(UpdatePatientProfileRequest request, CancellationToken cancellationToken)
    {
        var response = await updateMyPatientProfileUseCase.ExecuteAsync(User.GetUserId(), request, cancellationToken);

        if (response is null)
            return Problem(statusCode: StatusCodes.Status404NotFound, title: "Perfil de paciente não encontrado.");

        return Ok(response);
    }
}
