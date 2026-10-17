using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NogVita.Api.Extensions;
using NogVita.Application.NutritionPlans;
using NogVita.Domain.Users;
using static NogVita.Application.NutritionPlans.ManageNutritionPlanUseCase;

namespace NogVita.Api.Controllers;

[ApiController]
[Route("api/v1/nutritionists/me")]
[Authorize(Roles = Roles.Nutritionist)]
public sealed class NutritionistNutritionPlansController(
    CreateNutritionPlanUseCase createPlanUseCase,
    ManageNutritionPlanUseCase managePlanUseCase,
    NutritionistPlanQueriesUseCase planQueriesUseCase) : ControllerBase
{
    [HttpPost("nutrition-plans")]
    public async Task<IActionResult> Create(CreateNutritionPlanRequest request, CancellationToken cancellationToken)
    {
        var planId = await createPlanUseCase.ExecuteAsync(User.GetUserId(), request, cancellationToken);

        return planId is null
            ? Problem(statusCode: StatusCodes.Status404NotFound, title: "Paciente não encontrado.")
            : Created($"/api/v1/nutritionists/me/nutrition-plans/{planId}", new { id = planId });
    }

    [HttpGet("patients/{patientId:guid}/nutrition-plans")]
    public async Task<IActionResult> ListForPatient(
        Guid patientId, [FromQuery] NutritionPlanListRequest request, CancellationToken cancellationToken)
    {
        var response = await planQueriesUseCase.ListForPatientAsync(User.GetUserId(), patientId, request, cancellationToken);

        return response is null
            ? Problem(statusCode: StatusCodes.Status404NotFound, title: "Paciente não encontrado.")
            : Ok(response);
    }

    [HttpGet("nutrition-plans/{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var plan = await planQueriesUseCase.GetAsync(User.GetUserId(), id, cancellationToken);

        return plan is null ? PlanNotFound() : Ok(plan);
    }

    [HttpPut("nutrition-plans/{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateNutritionPlanRequest request, CancellationToken cancellationToken) =>
        ToActionResult(await managePlanUseCase.UpdateAsync(User.GetUserId(), id, request, cancellationToken));

    [HttpPost("nutrition-plans/{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken) =>
        ToActionResult(await managePlanUseCase.ActivateAsync(User.GetUserId(), id, cancellationToken));

    [HttpPost("nutrition-plans/{id:guid}/finish")]
    public async Task<IActionResult> Finish(Guid id, CancellationToken cancellationToken) =>
        ToActionResult(await managePlanUseCase.FinishAsync(User.GetUserId(), id, cancellationToken));

    [HttpPost("nutrition-plans/{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken) =>
        ToActionResult(await managePlanUseCase.CancelAsync(User.GetUserId(), id, cancellationToken));

    [HttpPut("nutrition-plans/{id:guid}/target")]
    public async Task<IActionResult> SetTarget(Guid id, PlanTargetRequest request, CancellationToken cancellationToken) =>
    ToActionResult(await managePlanUseCase.SetTargetAsync(User.GetUserId(), id, request, cancellationToken));

    [HttpDelete("nutrition-plans/{id:guid}/target")]
    public async Task<IActionResult> ClearTarget(Guid id, CancellationToken cancellationToken) =>
        ToActionResult(await managePlanUseCase.ClearTargetAsync(User.GetUserId(), id, cancellationToken));

    private IActionResult ToActionResult(ManageNutritionPlanResult result) => result switch
    {
        ManageNutritionPlanResult.Done => NoContent(),
        ManageNutritionPlanResult.InvalidState => Problem(
            statusCode: StatusCodes.Status409Conflict,
            title: "Esta ação não é permitida no status atual do plano."),
        _ => PlanNotFound()
    };

    private ObjectResult PlanNotFound() =>
        Problem(statusCode: StatusCodes.Status404NotFound, title: "Plano não encontrado.");
}