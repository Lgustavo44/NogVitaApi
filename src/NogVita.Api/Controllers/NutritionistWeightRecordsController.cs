using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NogVita.Api.Extensions;
using NogVita.Api.OpenApi;
using NogVita.Application.Common;
using NogVita.Application.Common.Pagination;
using NogVita.Application.WeightRecords;
using NogVita.Domain.Users;

namespace NogVita.Api.Controllers;

/// <summary>O registro de peso, do lado do nutricionista (o peso medido na consulta).</summary>
/// <remarks>Só para pacientes em acompanhamento ativo com este nutricionista; fora disso, 404.</remarks>
[ApiController]
[Route("api/v1/nutritionists/me")]
[Authorize(Roles = Roles.Nutritionist)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
[ProducesProblem(StatusCodes.Status500InternalServerError)]
public sealed class NutritionistWeightRecordsController(
    RecordWeightUseCase recordWeightUseCase,
    DeleteWeightRecordUseCase deleteWeightRecordUseCase,
    NutritionistWeightQueriesUseCase weightQueriesUseCase) : ControllerBase
{
    /// <summary>Registra o peso do paciente (o do dia, ou de uma data dos últimos 365 dias).</summary>
    /// <remarks>
    /// Um registro por dia: se o nutricionista já registrou esse dia, o registro é atualizado (200). Se quem registrou foi
    /// o paciente, ele não é sobrescrito (409).
    /// </remarks>
    /// <param name="patientId">O Id do paciente.</param>
    [HttpPost("patients/{patientId:guid}/weight-records")]
    [ProducesResponseType<IdResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<IdResponse>(StatusCodes.Status200OK)]
    [ProducesValidationProblem]
    [ProducesProblem(StatusCodes.Status404NotFound)]
    [ProducesProblem(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Record(Guid patientId, RecordWeightRequest request, CancellationToken cancellationToken)
    {
        var result = await recordWeightUseCase.RecordForPatientAsync(User.GetUserId(), patientId, request, cancellationToken);

        // O mesmo formato do PatientWeightRecordsController: { "id": ... }, e 200 quando corrige o próprio registro do dia.
        return result.Status switch
        {
            RecordWeightStatus.Created => Created($"/api/v1/nutritionists/me/patients/{patientId}/weight-records", new IdResponse(result.RecordId!.Value)),
            RecordWeightStatus.Updated => Ok(new IdResponse(result.RecordId!.Value)),
            RecordWeightStatus.RecordedByOther => Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Já existe um peso registrado neste dia pelo paciente."),
            RecordWeightStatus.NotFound => Problem(statusCode: StatusCodes.Status404NotFound, title: "Paciente não encontrado.")
        };
    }

    /// <summary>Lista o histórico de peso do paciente (inclusive o que ele mesmo registrou).</summary>
    /// <remarks>`from` e `to` são opcionais e inclusivos (`YYYY-MM-DD`).</remarks>
    /// <param name="patientId">O Id do paciente.</param>
    [HttpGet("patients/{patientId:guid}/weight-records")]
    [ProducesResponseType<PagedResponse<WeightRecordResponse>>(StatusCodes.Status200OK)]
    [ProducesValidationProblem]
    [ProducesProblem(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> List(Guid patientId, [FromQuery] WeightHistoryRequest request, CancellationToken cancellationToken)
    {
        var response = await weightQueriesUseCase.ListForPatientAsync(User.GetUserId(), patientId, request, cancellationToken);

        if (response is null)
        {
            return Problem(statusCode: StatusCodes.Status404NotFound, title: "Paciente não encontrado.");
        }

        return Ok(response);
    }

    /// <summary>Apaga um registro de peso feito pelo próprio nutricionista.</summary>
    /// <param name="id">O Id do registro (a rota não leva o Id do paciente).</param>
    [HttpDelete("weight-records/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesProblem(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken) =>
        await deleteWeightRecordUseCase.ExecuteAsync(User.GetUserId(), id, cancellationToken)
            ? NoContent()
            : Problem(statusCode: StatusCodes.Status404NotFound, title: "Registro de peso não encontrado.");
}
