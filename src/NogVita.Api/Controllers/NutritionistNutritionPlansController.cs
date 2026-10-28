using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NogVita.Api.Extensions;
using NogVita.Api.OpenApi;
using NogVita.Application.Common;
using NogVita.Application.Common.Pagination;
using NogVita.Application.NutritionPlans;
using NogVita.Domain.Users;
using static NogVita.Application.NutritionPlans.ManageNutritionPlanUseCase;

namespace NogVita.Api.Controllers;

/// <summary>Os planos alimentares, do lado do nutricionista.</summary>
/// <remarks>
/// Um plano nasce em `Draft` (rascunho), vira `Active` e termina em `Finished` (ou é `Cancelled`, se ainda era rascunho).
/// Só `Draft` e `Active` podem ser alterados. O nutricionista só alcança os planos que ele criou para pacientes que ainda acompanha.
/// </remarks>
[ApiController]
[Route("api/v1/nutritionists/me")]
[Authorize(Roles = Roles.Nutritionist)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
[ProducesProblem(StatusCodes.Status500InternalServerError)]
public sealed class NutritionistNutritionPlansController(
    CreateNutritionPlanUseCase createPlanUseCase,
    ManageNutritionPlanUseCase managePlanUseCase,
    PlanPdfUseCase planPdfUseCase,
    NutritionistPlanQueriesUseCase planQueriesUseCase) : ControllerBase
{
    /// <summary>Cria um plano em rascunho para um paciente acompanhado.</summary>
    [HttpPost("nutrition-plans")]
    [ProducesResponseType<IdResponse>(StatusCodes.Status201Created)]
    [ProducesValidationProblem]
    [ProducesProblem(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create(CreateNutritionPlanRequest request, CancellationToken cancellationToken)
    {
        var planId = await createPlanUseCase.ExecuteAsync(User.GetUserId(), request, cancellationToken);

        return planId is null
            ? Problem(statusCode: StatusCodes.Status404NotFound, title: "Paciente não encontrado.")
            : Created($"/api/v1/nutritionists/me/nutrition-plans/{planId}", new IdResponse(planId.Value));
    }

    /// <summary>Lista os planos que o nutricionista criou para um paciente.</summary>
    /// <param name="patientId">O Id do paciente.</param>
    [HttpGet("patients/{patientId:guid}/nutrition-plans")]
    [ProducesResponseType<PagedResponse<NutritionPlanResponse>>(StatusCodes.Status200OK)]
    [ProducesValidationProblem]
    [ProducesProblem(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ListForPatient(
        Guid patientId, [FromQuery] NutritionPlanListRequest request, CancellationToken cancellationToken)
    {
        var response = await planQueriesUseCase.ListForPatientAsync(User.GetUserId(), patientId, request, cancellationToken);

        return response is null
            ? Problem(statusCode: StatusCodes.Status404NotFound, title: "Paciente não encontrado.")
            : Ok(response);
    }

    /// <summary>Devolve o plano, com as refeições, os alimentos, os totais e a comparação com a meta.</summary>
    /// <remarks>Cada refeição traz `outsideRoutine`: `true` quando o horário cai enquanto o paciente dorme. É um aviso, e não um bloqueio.</remarks>
    /// <param name="id">O Id do plano.</param>
    [HttpGet("nutrition-plans/{id:guid}")]
    [ProducesResponseType<NutritionPlanDetailResponse>(StatusCodes.Status200OK)]
    [ProducesProblem(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var plan = await planQueriesUseCase.GetAsync(User.GetUserId(), id, cancellationToken);

        return plan is null ? PlanNotFound() : Ok(plan);
    }

    /// <summary>Baixa o plano em PDF.</summary>
    /// <param name="id">O Id do plano.</param>
    [HttpGet("nutrition-plans/{id:guid}/pdf")]
    [ProducesResponseType<Stream>(StatusCodes.Status200OK, "application/pdf")]
    [ProducesProblem(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadPdf(Guid id, CancellationToken cancellationToken)
    {
        var file = await planPdfUseCase.RenderForNutritionistAsync(User.GetUserId(), id, cancellationToken);

        return file is null ? PlanNotFound() : File(file.Content, "application/pdf", file.FileName);
    }

    /// <summary>Altera o nome, a descrição e as datas do plano.</summary>
    /// <param name="id">O Id do plano.</param>
    [HttpPut("nutrition-plans/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesValidationProblem]
    [ProducesProblem(StatusCodes.Status404NotFound)]
    [ProducesProblem(StatusCodes.Status409Conflict)]
    [ProducesProblem(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Update(Guid id, UpdateNutritionPlanRequest request, CancellationToken cancellationToken) =>
        ToActionResult(await managePlanUseCase.UpdateAsync(User.GetUserId(), id, request, cancellationToken));

    /// <summary>Ativa um rascunho. O plano ativo anterior do paciente, se houver, é finalizado na mesma operação.</summary>
    /// <param name="id">O Id do plano.</param>
    [HttpPost("nutrition-plans/{id:guid}/activate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesProblem(StatusCodes.Status404NotFound)]
    [ProducesProblem(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken) =>
        ToActionResult(await managePlanUseCase.ActivateAsync(User.GetUserId(), id, cancellationToken));

    /// <summary>Finaliza o plano ativo, com a data de hoje.</summary>
    /// <param name="id">O Id do plano.</param>
    [HttpPost("nutrition-plans/{id:guid}/finish")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesProblem(StatusCodes.Status404NotFound)]
    [ProducesProblem(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Finish(Guid id, CancellationToken cancellationToken) =>
        ToActionResult(await managePlanUseCase.FinishAsync(User.GetUserId(), id, cancellationToken));

    /// <summary>Cancela um rascunho. Um plano ativo não pode ser cancelado, só finalizado.</summary>
    /// <param name="id">O Id do plano.</param>
    [HttpPost("nutrition-plans/{id:guid}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesProblem(StatusCodes.Status404NotFound)]
    [ProducesProblem(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken) =>
        ToActionResult(await managePlanUseCase.CancelAsync(User.GetUserId(), id, cancellationToken));

    /// <summary>Define a meta diária: as calorias, e opcionalmente a proteína, o carboidrato e a gordura.</summary>
    /// <remarks>O detalhe do plano passa a comparar os totais do dia com a meta.</remarks>
    /// <param name="id">O Id do plano.</param>
    [HttpPut("nutrition-plans/{id:guid}/target")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesValidationProblem]
    [ProducesProblem(StatusCodes.Status404NotFound)]
    [ProducesProblem(StatusCodes.Status409Conflict)]
    [ProducesProblem(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> SetTarget(Guid id, PlanTargetRequest request, CancellationToken cancellationToken) =>
        ToActionResult(await managePlanUseCase.SetTargetAsync(User.GetUserId(), id, request, cancellationToken));

    /// <summary>Remove a meta diária do plano.</summary>
    /// <param name="id">O Id do plano.</param>
    [HttpDelete("nutrition-plans/{id:guid}/target")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesProblem(StatusCodes.Status404NotFound)]
    [ProducesProblem(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ClearTarget(Guid id, CancellationToken cancellationToken) =>
        ToActionResult(await managePlanUseCase.ClearTargetAsync(User.GetUserId(), id, cancellationToken));

    private IActionResult ToActionResult(ManageNutritionPlanResult result) => result switch
    {
        ManageNutritionPlanResult.Done => NoContent(),
        ManageNutritionPlanResult.InvalidState => Problem(
            statusCode: StatusCodes.Status409Conflict,
            title: "Esta ação não é permitida no status atual do plano."),
        ManageNutritionPlanResult.NotFound => PlanNotFound()
    };

    private ObjectResult PlanNotFound() =>
        Problem(statusCode: StatusCodes.Status404NotFound, title: "Plano não encontrado.");
}
