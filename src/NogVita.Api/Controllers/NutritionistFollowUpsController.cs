using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NogVita.Api.Extensions;
using NogVita.Application.FollowUps;
using NogVita.Domain.Users;

namespace NogVita.Api.Controllers;

[ApiController]
[Route("api/v1/nutritionists/me")]
[Authorize(Roles = Roles.Nutritionist)]
public sealed class NutritionistFollowUpsController(
    RespondNutritionistRequestUseCase respondUseCase,
    EndCareRelationshipUseCase endCareRelationshipUseCase,
    GetFollowedPatientUseCase getFollowedPatientUseCase,
    IFollowUpQueries followUpQueries) : ControllerBase
{
    [HttpGet("requests")]
    public async Task<IActionResult> ListRequests([FromQuery] NutritionistRequestListRequest request, CancellationToken cancellationToken)
    {
        var response = await followUpQueries.ListNutritionistRequestsAsync(User.GetUserId(), request, cancellationToken);

        return Ok(response);
    }

    [HttpPost("requests/{id:guid}/accept")]
    public async Task<IActionResult> Accept(Guid id, CancellationToken cancellationToken)
    {
        var result = await respondUseCase.AcceptAsync(User.GetUserId(), id, cancellationToken);

        return ToActionResult(result);
    }

    [HttpPost("requests/{id:guid}/reject")]
    public async Task<IActionResult> Reject(Guid id, CancellationToken cancellationToken)
    {
        var result = await respondUseCase.RejectAsync(User.GetUserId(), id, cancellationToken);

        return ToActionResult(result);
    }

    [HttpGet("patients")]
    public async Task<IActionResult> ListPatients([FromQuery] MyPatientsListRequest request, CancellationToken cancellationToken)
    {
        var response = await followUpQueries.ListMyPatientsAsync(User.GetUserId(), request, cancellationToken);

        return Ok(response);
    }

    [HttpGet("patients/{patientId:guid}")]
    public async Task<IActionResult> GetPatient(Guid patientId, CancellationToken cancellationToken)
    {
        var patient = await getFollowedPatientUseCase.ExecuteAsync(User.GetUserId(), patientId, cancellationToken);

        return patient is null
            ? Problem(statusCode: StatusCodes.Status404NotFound, title: "Paciente não encontrado.")
            : Ok(patient);
    }

    [HttpPost("patients/{patientId:guid}/end")]
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
        _ => Problem(statusCode: StatusCodes.Status404NotFound, title: "Solicitação não encontrada.")
    };
}