using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NogVita.Api.Extensions;
using NogVita.Application.WeightRecords;
using NogVita.Domain.Users;

namespace NogVita.Api.Controllers;

[ApiController]
[Route("api/v1/patients/me/weight-records")]
[Authorize(Roles = Roles.Patient)]
public sealed class PatientWeightRecordsController(
    RecordWeightUseCase recordWeightUseCase,
    DeleteWeightRecordUseCase deleteWeightRecordUseCase,
    IWeightRecordQueries weightQueries) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Record(RecordWeightRequest request, CancellationToken cancellationToken)
    {
        var result = await recordWeightUseCase.RecordForSelfAsync(User.GetUserId(), request, cancellationToken);

        return result.Status switch
        {
            RecordWeightStatus.Created => Created("/api/v1/patients/me/weight-records", new { id = result.RecordId }),
            RecordWeightStatus.Updated => Ok(new { id = result.RecordId }),
            RecordWeightStatus.RecordedByOther => Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Já existe um peso registrado neste dia pelo seu nutricionista."),
            RecordWeightStatus.NotFound => Problem(statusCode: StatusCodes.Status404NotFound, title: "Perfil de paciente não encontrado.")
        };
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] WeightHistoryRequest request, CancellationToken cancellationToken) =>
        Ok(await weightQueries.ListByPatientAsync(User.GetUserId(), request, cancellationToken));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken) =>
        await deleteWeightRecordUseCase.ExecuteAsync(User.GetUserId(), id, cancellationToken)
            ? NoContent()
            : Problem(statusCode: StatusCodes.Status404NotFound, title: "Registro de peso não encontrado.");
}