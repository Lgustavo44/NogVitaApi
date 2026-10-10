using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NogVita.Api.Extensions;
using NogVita.Application.FollowUps;
using NogVita.Domain.Users;

namespace NogVita.Api.Controllers;

[ApiController]
[Route("api/v1/patients/me")]
[Authorize(Roles = Roles.Patient)]
public sealed class PatientFollowUpsController(
    RequestNutritionistUseCase requestNutritionistUseCase,
    CancelNutritionistRequestUseCase cancelRequestUseCase,
    GetMyCareRelationshipUseCase getMyCareRelationshipUseCase,
    EndCareRelationshipUseCase endCareRelationshipUseCase,
    IFollowUpQueries followUpQueries) : ControllerBase
{
    [HttpPost("nutritionist-requests")]
    public async Task<IActionResult> Request(RequestNutritionistRequest request, CancellationToken cancellationToken)
    {
        var result = await requestNutritionistUseCase.ExecuteAsync(User.GetUserId(), request, cancellationToken);

        return result.Status switch
        {
            RequestNutritionistStatus.Created =>
                Created("/api/v1/patients/me/nutritionist-requests", new { id = result.RequestId }),
            RequestNutritionistStatus.CannotRequestSelf =>
                Problem(statusCode: StatusCodes.Status400BadRequest, title: "Você não pode solicitar acompanhamento a si mesmo."),
            RequestNutritionistStatus.NutritionistNotFound =>
                Problem(statusCode: StatusCodes.Status404NotFound, title: "Nutricionista não encontrado."),
            RequestNutritionistStatus.AlreadyHasNutritionist =>
                Problem(statusCode: StatusCodes.Status409Conflict, title: "Você já está em acompanhamento. Encerre o atual antes de solicitar outro."),
            RequestNutritionistStatus.PendingRequestExists =>
                Problem(statusCode: StatusCodes.Status409Conflict, title: "Você já tem uma solicitação pendente."),
            _ =>
                Problem(statusCode: StatusCodes.Status404NotFound, title: "Perfil de paciente não encontrado.")
        };
    }

    [HttpGet("nutritionist-requests")]
    public async Task<IActionResult> ListRequests([FromQuery] NutritionistRequestListRequest request, CancellationToken cancellationToken)
    {
        var response = await followUpQueries.ListPatientRequestsAsync(User.GetUserId(), request, cancellationToken);

        return Ok(response);
    }

    [HttpPost("nutritionist-requests/{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        var result = await cancelRequestUseCase.ExecuteAsync(User.GetUserId(), id, cancellationToken);

        return result switch
        {
            CancelNutritionistRequestResult.Cancelled => NoContent(),
            CancelNutritionistRequestResult.NotPending =>
                Problem(statusCode: StatusCodes.Status409Conflict, title: "Esta solicitação não está mais pendente."),
            _ => Problem(statusCode: StatusCodes.Status404NotFound, title: "Solicitação não encontrada.")
        };
    }

    [HttpGet("care-relationship")]
    public async Task<IActionResult> GetCareRelationship(CancellationToken cancellationToken)
    {
        var response = await getMyCareRelationshipUseCase.ExecuteAsync(User.GetUserId(), cancellationToken);

        return response is null
            ? Problem(statusCode: StatusCodes.Status404NotFound, title: "Você não está em acompanhamento.")
            : Ok(response);
    }

    [HttpPost("care-relationship/end")]
    public async Task<IActionResult> EndCareRelationship(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var ended = await endCareRelationshipUseCase.ExecuteAsync(userId, userId, cancellationToken);

        return ended
            ? NoContent()
            : Problem(statusCode: StatusCodes.Status404NotFound, title: "Você não está em acompanhamento.");
    }
}