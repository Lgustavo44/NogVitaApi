using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NogVita.Api.Extensions;
using NogVita.Api.OpenApi;
using NogVita.Application.Anamneses;
using NogVita.Application.Common;
using NogVita.Application.Common.Pagination;
using NogVita.Domain.Users;

namespace NogVita.Api.Controllers;

/// <summary>As anamneses dos pacientes, do lado do nutricionista.</summary>
/// <remarks>Só com o acompanhamento ativo: sem ele, as rotas respondem 404, inclusive para as anamneses já respondidas.</remarks>
[ApiController]
[Route("api/v1/nutritionists/me")]
[Authorize(Roles = Roles.Nutritionist)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
[ProducesProblem(StatusCodes.Status500InternalServerError)]
public sealed class NutritionistAnamnesesController(
    AnamnesisUseCase anamnesisUseCase,
    NutritionistAnamnesisQueriesUseCase queriesUseCase) : ControllerBase
{
    /// <summary>Envia um questionário ao paciente, que passa a ter uma anamnese pendente.</summary>
    /// <param name="patientId">O Id do paciente.</param>
    [HttpPost("patients/{patientId:guid}/anamneses")]
    [ProducesResponseType<IdResponse>(StatusCodes.Status201Created)]
    [ProducesValidationProblem]
    [ProducesProblem(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Send(Guid patientId, SendAnamnesisRequest request, CancellationToken cancellationToken)
    {
        var result = await anamnesisUseCase.SendAsync(User.GetUserId(), patientId, request, cancellationToken);

        return result.Status switch
        {
            SendAnamnesisStatus.Sent => Created($"/api/v1/nutritionists/me/anamneses/{result.AnamnesisId}", new IdResponse(result.AnamnesisId!.Value)),
            SendAnamnesisStatus.QuestionnaireNotFound => Problem(statusCode: StatusCodes.Status404NotFound, title: "Questionário não encontrado."),
            SendAnamnesisStatus.PatientNotFound => PatientNotFound()
        };
    }

    /// <summary>Lista as anamneses que o nutricionista enviou ao paciente, da mais recente para a mais antiga.</summary>
    /// <param name="patientId">O Id do paciente.</param>
    [HttpGet("patients/{patientId:guid}/anamneses")]
    [ProducesResponseType<PagedResponse<AnamnesisSummaryResponse>>(StatusCodes.Status200OK)]
    [ProducesValidationProblem]
    [ProducesProblem(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ListForPatient(
        Guid patientId, [FromQuery] AnamnesisListRequest request, CancellationToken cancellationToken)
    {
        var response = await queriesUseCase.ListForPatientAsync(User.GetUserId(), patientId, request, cancellationToken);

        return response is null ? PatientNotFound() : Ok(response);
    }

    /// <summary>Devolve a anamnese, com as perguntas e, se o paciente já respondeu, as respostas.</summary>
    /// <param name="id">O Id da anamnese.</param>
    [HttpGet("anamneses/{id:guid}")]
    [ProducesResponseType<AnamnesisResponse>(StatusCodes.Status200OK)]
    [ProducesProblem(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var anamnesis = await queriesUseCase.GetForNutritionistAsync(User.GetUserId(), id, cancellationToken);

        return anamnesis is null
            ? Problem(statusCode: StatusCodes.Status404NotFound, title: "Anamnese não encontrada.")
            : Ok(anamnesis);
    }

    private ObjectResult PatientNotFound() =>
        Problem(statusCode: StatusCodes.Status404NotFound, title: "Paciente não encontrado.");
}
