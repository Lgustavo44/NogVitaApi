using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NogVita.Api.Extensions;
using NogVita.Api.OpenApi;
using NogVita.Application.Common;
using NogVita.Application.FoodDiary;
using NogVita.Domain.Users;

namespace NogVita.Api.Controllers;

/// <summary>O diário alimentar do paciente: o check-in de cada refeição do plano, os alimentos fora do plano e o check-in da semana.</summary>
/// <remarks>
/// As escritas exigem o aceite do termo dos dados de saúde (422 sem ele) e valem de hoje até 7 dias atrás (422 fora disso).
/// As datas vão no formato `AAAA-MM-DD`, no horário de Brasília. Os macros seguem a regra do plano (`patientSeesMacros`).
/// </remarks>
[ApiController]
[Route("api/v1/patients/me")]
[Authorize(Roles = Roles.Patient)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
[ProducesProblem(StatusCodes.Status500InternalServerError)]
public sealed class PatientFoodDiaryController(
    FoodDiaryReader reader,
    FoodDiaryUseCase diaryUseCase,
    WeeklyCheckInUseCase weeklyUseCase) : ControllerBase
{
    /// <summary>Devolve os dias registrados no período (padrão: os últimos 7 dias), com a adesão às refeições.</summary>
    [HttpGet("diary")]
    [ProducesResponseType<DiaryRangeResponse>(StatusCodes.Status200OK)]
    [ProducesValidationProblem]
    public async Task<IActionResult> List([FromQuery] DiaryRangeRequest request, CancellationToken cancellationToken) =>
        Ok(await reader.GetRangeForPatientAsync(User.GetUserId(), request, cancellationToken));

    /// <summary>Devolve um dia: as refeições do plano ativo (respondidas ou não), os extras, o planejado e o consumido estimado.</summary>
    /// <param name="date">O dia, em `AAAA-MM-DD`.</param>
    [HttpGet("diary/{date}")]
    [ProducesResponseType<DiaryDayResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDay(DateOnly date, CancellationToken cancellationToken) =>
        Ok(await reader.GetDayForPatientAsync(User.GetUserId(), date, cancellationToken));

    /// <summary>Responde uma refeição do plano ativo: seguiu (`Followed`), em parte (`Partial`) ou não seguiu (`Skipped`).</summary>
    /// <remarks>Responder de novo substitui a resposta anterior. O planejado da refeição é guardado como estava agora.</remarks>
    /// <param name="date">O dia, em `AAAA-MM-DD`.</param>
    /// <param name="mealId">O Id da refeição do plano ativo.</param>
    [HttpPut("diary/{date}/meals/{mealId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesValidationProblem]
    [ProducesProblem(StatusCodes.Status404NotFound)]
    [ProducesProblem(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CheckInMeal(DateOnly date, Guid mealId, CheckInMealRequest request, CancellationToken cancellationToken) =>
        ToActionResult((await diaryUseCase.CheckInMealAsync(User.GetUserId(), date, mealId, request, cancellationToken)).Result);

    /// <summary>Apaga a resposta de uma refeição do dia.</summary>
    /// <param name="date">O dia, em `AAAA-MM-DD`.</param>
    /// <param name="mealId">O Id da refeição.</param>
    [HttpDelete("diary/{date}/meals/{mealId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesProblem(StatusCodes.Status404NotFound)]
    [ProducesProblem(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ClearMealCheckIn(DateOnly date, Guid mealId, CancellationToken cancellationToken) =>
        ToActionResult(await diaryUseCase.ClearMealCheckInAsync(User.GetUserId(), date, mealId, cancellationToken));

    /// <summary>Registra um alimento comido fora do plano, em gramas ou em porções, numa refeição ou fora delas (`mealId` nulo).</summary>
    /// <param name="date">O dia, em `AAAA-MM-DD`.</param>
    [HttpPost("diary/{date}/extras")]
    [ProducesResponseType<IdResponse>(StatusCodes.Status201Created)]
    [ProducesValidationProblem]
    [ProducesProblem(StatusCodes.Status404NotFound)]
    [ProducesProblem(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> AddExtra(DateOnly date, AddExtraFoodRequest request, CancellationToken cancellationToken)
    {
        var outcome = await diaryUseCase.AddExtraFoodAsync(User.GetUserId(), date, request, cancellationToken);

        return outcome.Result == FoodDiaryResult.Done
            ? Created($"/api/v1/patients/me/diary/{date:yyyy-MM-dd}", new IdResponse(outcome.Id!.Value))
            : ToActionResult(outcome.Result);
    }

    /// <summary>Apaga um alimento fora do plano.</summary>
    /// <param name="date">O dia, em `AAAA-MM-DD`.</param>
    /// <param name="extraId">O Id do alimento registrado.</param>
    [HttpDelete("diary/{date}/extras/{extraId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesProblem(StatusCodes.Status404NotFound)]
    [ProducesProblem(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> RemoveExtra(DateOnly date, Guid extraId, CancellationToken cancellationToken) =>
        ToActionResult(await diaryUseCase.RemoveExtraFoodAsync(User.GetUserId(), date, extraId, cancellationToken));

    /// <summary>Lista os check-ins semanais do paciente (as últimas 12 semanas), com as devolutivas do nutricionista.</summary>
    [HttpGet("weekly-check-ins")]
    [ProducesResponseType<IReadOnlyList<WeeklyCheckInResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListWeeks(CancellationToken cancellationToken) =>
        Ok(await weeklyUseCase.ListForPatientAsync(User.GetUserId(), cancellationToken));

    /// <summary>Cria ou altera o check-in da semana (que começa na segunda-feira informada).</summary>
    /// <remarks>Depois da devolutiva do nutricionista, o check-in fica travado (422). Até 4 semanas atrás.</remarks>
    /// <param name="weekStart">A segunda-feira da semana, em `AAAA-MM-DD`.</param>
    [HttpPut("weekly-check-ins/{weekStart}")]
    [ProducesResponseType<WeeklyCheckInResponse>(StatusCodes.Status200OK)]
    [ProducesValidationProblem]
    [ProducesProblem(StatusCodes.Status404NotFound)]
    [ProducesProblem(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> SaveWeek(DateOnly weekStart, WeeklyCheckInRequest request, CancellationToken cancellationToken)
    {
        var outcome = await weeklyUseCase.SaveForPatientAsync(User.GetUserId(), weekStart, request, cancellationToken);

        return outcome.Result == WeeklyCheckInResult.Done
            ? Ok(outcome.CheckIn)
            : Problem(statusCode: StatusCodes.Status404NotFound, title: "Perfil de paciente não encontrado.");
    }

    private IActionResult ToActionResult(FoodDiaryResult result) => result switch
    {
        FoodDiaryResult.Done => NoContent(),
        FoodDiaryResult.PatientNotFound => Problem(statusCode: StatusCodes.Status404NotFound, title: "Perfil de paciente não encontrado."),
        FoodDiaryResult.NoActivePlan => Problem(statusCode: StatusCodes.Status404NotFound, title: "Você não tem um plano ativo."),
        FoodDiaryResult.MealNotFound => Problem(statusCode: StatusCodes.Status404NotFound, title: "Refeição não encontrada no seu plano ativo."),
        FoodDiaryResult.FoodNotFound => Problem(statusCode: StatusCodes.Status404NotFound, title: "Alimento não encontrado ou indisponível."),
        FoodDiaryResult.PortionNotFound => Problem(statusCode: StatusCodes.Status404NotFound, title: "Porção não encontrada para este alimento."),
        _ => Problem(statusCode: StatusCodes.Status404NotFound, title: "Registro não encontrado no diário.")
    };
}
