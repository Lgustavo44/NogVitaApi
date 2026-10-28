using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NogVita.Api.Extensions;
using NogVita.Api.OpenApi;
using NogVita.Application.Common.Pagination;
using NogVita.Application.FollowUps;
using NogVita.Domain.Users;

namespace NogVita.Api.Controllers;

/// <summary>O acompanhamento, do lado do nutricionista: responder pedidos e ver os pacientes.</summary>
/// <remarks>O nutricionista só alcança os pacientes que acompanha. Um paciente de outro, ou já sem vínculo, responde 404.</remarks>
[ApiController]
[Route("api/v1/nutritionists/me")]
[Authorize(Roles = Roles.Nutritionist)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
[ProducesProblem(StatusCodes.Status500InternalServerError)]
public sealed class NutritionistFollowUpsController(
    RespondNutritionistRequestUseCase respondUseCase,
    EndCareRelationshipUseCase endCareRelationshipUseCase,
    GetFollowedPatientUseCase getFollowedPatientUseCase,
    IFollowUpQueries followUpQueries) : ControllerBase
{
    /// <summary>Lista as solicitações recebidas, da mais recente para a mais antiga.</summary>
    [HttpGet("requests")]
    [ProducesResponseType<PagedResponse<NutritionistRequestItem>>(StatusCodes.Status200OK)]
    [ProducesValidationProblem]
    public async Task<IActionResult> ListRequests([FromQuery] NutritionistRequestListRequest request, CancellationToken cancellationToken)
    {
        var response = await followUpQueries.ListNutritionistRequestsAsync(User.GetUserId(), request, cancellationToken);

        return Ok(response);
    }

    /// <summary>Aceita uma solicitação pendente: começa o acompanhamento.</summary>
    /// <param name="id">O Id da solicitação.</param>
    [HttpPost("requests/{id:guid}/accept")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesProblem(StatusCodes.Status404NotFound)]
    [ProducesProblem(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Accept(Guid id, CancellationToken cancellationToken)
    {
        var result = await respondUseCase.AcceptAsync(User.GetUserId(), id, cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>Recusa uma solicitação pendente.</summary>
    /// <param name="id">O Id da solicitação.</param>
    [HttpPost("requests/{id:guid}/reject")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesProblem(StatusCodes.Status404NotFound)]
    [ProducesProblem(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Reject(Guid id, CancellationToken cancellationToken)
    {
        var result = await respondUseCase.RejectAsync(User.GetUserId(), id, cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>Lista os pacientes em acompanhamento ativo, em ordem alfabética.</summary>
    /// <remarks>`search` busca no nome. Pacientes desativados pelo administrador não aparecem.</remarks>
    [HttpGet("patients")]
    [ProducesResponseType<PagedResponse<MyPatientItem>>(StatusCodes.Status200OK)]
    [ProducesValidationProblem]
    public async Task<IActionResult> ListPatients([FromQuery] MyPatientsListRequest request, CancellationToken cancellationToken)
    {
        var response = await followUpQueries.ListMyPatientsAsync(User.GetUserId(), request, cancellationToken);

        return Ok(response);
    }

    /// <summary>Devolve o perfil de um paciente acompanhado, com o peso mais recente e o IMC.</summary>
    /// <remarks>Traz a idade, e não a data de nascimento, e nunca o CPF ou o e-mail.</remarks>
    /// <param name="patientId">O Id do paciente (da lista de pacientes).</param>
    [HttpGet("patients/{patientId:guid}")]
    [ProducesResponseType<FollowedPatientResponse>(StatusCodes.Status200OK)]
    [ProducesProblem(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPatient(Guid patientId, CancellationToken cancellationToken)
    {
        var patient = await getFollowedPatientUseCase.ExecuteAsync(User.GetUserId(), patientId, cancellationToken);

        return patient is null
            ? Problem(statusCode: StatusCodes.Status404NotFound, title: "Paciente não encontrado.")
            : Ok(patient);
    }

    /// <summary>Encerra o acompanhamento de um paciente.</summary>
    /// <remarks>O paciente recebe um e-mail. O plano ativo é finalizado, e os rascunhos são cancelados.</remarks>
    /// <param name="patientId">O Id do paciente, e não o da solicitação.</param>
    [HttpPost("patients/{patientId:guid}/end")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesProblem(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> End(Guid patientId, CancellationToken cancellationToken)
    {
        var result = await endCareRelationshipUseCase.ExecuteAsync(User.GetUserId(), patientId, cancellationToken);

        return result
            ? NoContent()
            : Problem(statusCode: StatusCodes.Status404NotFound, title: "Acompanhamento não encontrado.");
    }

    private IActionResult ToActionResult(RespondNutritionistRequestResult result) => result switch
    {
        RespondNutritionistRequestResult.Done => NoContent(),
        RespondNutritionistRequestResult.NotPending =>
            Problem(statusCode: StatusCodes.Status409Conflict, title: "Esta solicitação não está mais pendente."),
        RespondNutritionistRequestResult.NotFound => Problem(statusCode: StatusCodes.Status404NotFound, title: "Solicitação não encontrada.")
    };
}
