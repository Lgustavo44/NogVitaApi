using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NogVita.Api.Extensions;
using NogVita.Application.WeightRecords;
using NogVita.Domain.Users;

namespace NogVita.Api.Controllers;

[ApiController]
[Route("api/v1/nutritionists/me")]
[Authorize(Roles = Roles.Nutritionist)]
public sealed class NutritionistWeightRecordsController(
    RecordWeightUseCase recordWeightUseCase,
    DeleteWeightRecordUseCase deleteWeightRecordUseCase,
    NutritionistWeightQueriesUseCase weightQueriesUseCase) : ControllerBase
{
    [HttpPost("patients/{patientId:guid}/weight-records")]
    public async Task<IActionResult> Record(Guid patientId, RecordWeightRequest request, CancellationToken cancellationToken)
    {
        var result = await recordWeightUseCase.RecordForPatientAsync(User.GetUserId(), patientId, request, cancellationToken);

        // O mesmo formato do PatientWeightRecordsController: { "id": ... }, e 200 quando corrige o próprio registro do dia.
        return result.Status switch
        {
            RecordWeightStatus.Created => Created($"/api/v1/nutritionists/me/patients/{patientId}/weight-records", new { id = result.RecordId }),
            RecordWeightStatus.Updated => Ok(new { id = result.RecordId }),
            RecordWeightStatus.RecordedByOther => Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Já existe um peso registrado neste dia pelo paciente."),
            RecordWeightStatus.NotFound => Problem(statusCode: StatusCodes.Status404NotFound, title: "Paciente não encontrado.")
        };
    }

    [HttpGet("patients/{patientId:guid}/weight-records")]
    public async Task<IActionResult> List(Guid patientId, [FromQuery] WeightHistoryRequest request, CancellationToken cancellationToken)
    {
        var response = await weightQueriesUseCase.ListForPatientAsync(User.GetUserId(), patientId, request, cancellationToken);

        if (response is null)
        {
            return Problem(statusCode: StatusCodes.Status404NotFound, title: "Paciente não encontrado.");
        }

        return Ok(response);
    }

    [HttpDelete("weight-records/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken) =>
        await deleteWeightRecordUseCase.ExecuteAsync(User.GetUserId(), id, cancellationToken)
            ? NoContent()
            : Problem(statusCode: StatusCodes.Status404NotFound, title: "Registro de peso não encontrado.");
}