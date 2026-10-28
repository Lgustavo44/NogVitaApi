using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NogVita.Api.Extensions;
using NogVita.Api.OpenApi;
using NogVita.Application.Common;
using NogVita.Application.Common.Pagination;
using NogVita.Application.WeightRecords;
using NogVita.Domain.Users;

namespace NogVita.Api.Controllers;

/// <summary>O registro de peso, do lado do paciente.</summary>
[ApiController]
[Route("api/v1/patients/me/weight-records")]
[Authorize(Roles = Roles.Patient)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
[ProducesProblem(StatusCodes.Status500InternalServerError)]
public sealed class PatientWeightRecordsController(
    RecordWeightUseCase recordWeightUseCase,
    DeleteWeightRecordUseCase deleteWeightRecordUseCase,
    IWeightRecordQueries weightQueries) : ControllerBase
{
    /// <summary>Registra o peso do dia (ou de uma data dos últimos 365 dias).</summary>
    /// <remarks>
    /// Um registro por dia: se o paciente já registrou esse dia, o registro é atualizado (200). Se quem registrou foi o
    /// nutricionista, ele não é sobrescrito (409). Sem `recordedOn`, vale a data de hoje (horário de Brasília).
    /// </remarks>
    [HttpPost]
    [ProducesResponseType<IdResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<IdResponse>(StatusCodes.Status200OK)]
    [ProducesValidationProblem]
    [ProducesProblem(StatusCodes.Status404NotFound)]
    [ProducesProblem(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Record(RecordWeightRequest request, CancellationToken cancellationToken)
    {
        var result = await recordWeightUseCase.RecordForSelfAsync(User.GetUserId(), request, cancellationToken);

        return result.Status switch
        {
            RecordWeightStatus.Created => Created("/api/v1/patients/me/weight-records", new IdResponse(result.RecordId!.Value)),
            RecordWeightStatus.Updated => Ok(new IdResponse(result.RecordId!.Value)),
            RecordWeightStatus.RecordedByOther => Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Já existe um peso registrado neste dia pelo seu nutricionista."),
            RecordWeightStatus.NotFound => Problem(statusCode: StatusCodes.Status404NotFound, title: "Perfil de paciente não encontrado.")
        };
    }

    /// <summary>Lista o histórico de peso, do dia mais recente para o mais antigo.</summary>
    /// <remarks>`from` e `to` são opcionais e inclusivos (`YYYY-MM-DD`).</remarks>
    [HttpGet]
    [ProducesResponseType<PagedResponse<WeightRecordResponse>>(StatusCodes.Status200OK)]
    [ProducesValidationProblem]
    public async Task<IActionResult> List([FromQuery] WeightHistoryRequest request, CancellationToken cancellationToken) =>
        Ok(await weightQueries.ListByPatientAsync(User.GetUserId(), request, cancellationToken));

    /// <summary>Apaga um registro de peso feito pelo próprio paciente.</summary>
    /// <remarks>Um registro feito pelo nutricionista responde 404: só quem registrou pode apagar.</remarks>
    /// <param name="id">O Id do registro.</param>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesProblem(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken) =>
        await deleteWeightRecordUseCase.ExecuteAsync(User.GetUserId(), id, cancellationToken)
            ? NoContent()
            : Problem(statusCode: StatusCodes.Status404NotFound, title: "Registro de peso não encontrado.");
}
