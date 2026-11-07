using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NogVita.Api.Extensions;
using NogVita.Api.OpenApi;
using NogVita.Application.FoodDiary;
using NogVita.Domain.Users;

namespace NogVita.Api.Controllers;

/// <summary>O diário alimentar e os check-ins semanais dos pacientes, do lado do nutricionista.</summary>
/// <remarks>Só com o acompanhamento ativo: sem ele, as rotas respondem 404.</remarks>
[ApiController]
[Route("api/v1/nutritionists/me")]
[Authorize(Roles = Roles.Nutritionist)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
[ProducesProblem(StatusCodes.Status500InternalServerError)]
public sealed class NutritionistFoodDiaryController(FoodDiaryReader reader, WeeklyCheckInUseCase weeklyUseCase) : ControllerBase
{
    /// <summary>Devolve o diário do paciente no período (padrão: os últimos 7 dias), com a adesão às refeições.</summary>
    /// <param name="patientId">O Id do paciente.</param>
    [HttpGet("patients/{patientId:guid}/diary")]
    [ProducesResponseType<DiaryRangeResponse>(StatusCodes.Status200OK)]
    [ProducesValidationProblem]
    [ProducesProblem(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDiary(Guid patientId, [FromQuery] DiaryRangeRequest request, CancellationToken cancellationToken)
    {
        var diary = await reader.GetRangeForNutritionistAsync(User.GetUserId(), patientId, request, cancellationToken);

        return diary is null ? PatientNotFound() : Ok(diary);
    }

    /// <summary>Lista os check-ins semanais do paciente (as últimas 12 semanas).</summary>
    /// <param name="patientId">O Id do paciente.</param>
    [HttpGet("patients/{patientId:guid}/weekly-check-ins")]
    [ProducesResponseType<IReadOnlyList<WeeklyCheckInResponse>>(StatusCodes.Status200OK)]
    [ProducesProblem(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ListWeeks(Guid patientId, CancellationToken cancellationToken)
    {
        var weeks = await weeklyUseCase.ListForNutritionistAsync(User.GetUserId(), patientId, cancellationToken);

        return weeks is null ? PatientNotFound() : Ok(weeks);
    }

    /// <summary>Lista os check-ins semanais dos pacientes acompanhados que ainda esperam a devolutiva.</summary>
    [HttpGet("weekly-check-ins/awaiting-feedback")]
    [ProducesResponseType<IReadOnlyList<AwaitingFeedbackResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListAwaitingFeedback(CancellationToken cancellationToken) =>
        Ok(await weeklyUseCase.ListAwaitingFeedbackAsync(User.GetUserId(), cancellationToken));

    /// <summary>Escreve (ou corrige) a devolutiva de um check-in semanal. Depois dela, o paciente não altera mais o check-in.</summary>
    /// <param name="id">O Id do check-in semanal.</param>
    [HttpPut("weekly-check-ins/{id:guid}/feedback")]
    [ProducesResponseType<WeeklyCheckInResponse>(StatusCodes.Status200OK)]
    [ProducesValidationProblem]
    [ProducesProblem(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GiveFeedback(Guid id, WeeklyFeedbackRequest request, CancellationToken cancellationToken)
    {
        var outcome = await weeklyUseCase.GiveFeedbackAsync(User.GetUserId(), id, request, cancellationToken);

        return outcome.Result == WeeklyCheckInResult.Done
            ? Ok(outcome.CheckIn)
            : Problem(statusCode: StatusCodes.Status404NotFound, title: "Check-in semanal não encontrado.");
    }

    private ObjectResult PatientNotFound() =>
        Problem(statusCode: StatusCodes.Status404NotFound, title: "Paciente não encontrado.");
}
