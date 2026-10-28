using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NogVita.Api.Extensions;
using NogVita.Api.OpenApi;
using NogVita.Application.Common;
using NogVita.Application.Common.Pagination;
using NogVita.Application.FollowUps;
using NogVita.Domain.Users;

namespace NogVita.Api.Controllers;

/// <summary>O acompanhamento, do lado do paciente: pedir um nutricionista, acompanhar o pedido e encerrar.</summary>
[ApiController]
[Route("api/v1/patients/me")]
[Authorize(Roles = Roles.Patient)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
[ProducesProblem(StatusCodes.Status500InternalServerError)]
public sealed class PatientFollowUpsController(
    RequestNutritionistUseCase requestNutritionistUseCase,
    CancelNutritionistRequestUseCase cancelRequestUseCase,
    GetMyCareRelationshipUseCase getMyCareRelationshipUseCase,
    EndCareRelationshipUseCase endCareRelationshipUseCase,
    IFollowUpQueries followUpQueries) : ControllerBase
{
    /// <summary>Pede acompanhamento a um nutricionista da lista.</summary>
    /// <remarks>
    /// O paciente tem no máximo um acompanhamento ativo e uma solicitação pendente por vez (409 nos dois casos).
    /// Não é possível pedir acompanhamento para si mesmo (400). A mensagem é opcional, com até 500 caracteres.
    /// </remarks>
    [HttpPost("nutritionist-requests")]
    [ProducesResponseType<IdResponse>(StatusCodes.Status201Created)]
    [ProducesValidationProblem]
    [ProducesProblem(StatusCodes.Status400BadRequest)]
    [ProducesProblem(StatusCodes.Status404NotFound)]
    [ProducesProblem(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RequestNutritionist(RequestNutritionistRequest request, CancellationToken cancellationToken)
    {
        var result = await requestNutritionistUseCase.ExecuteAsync(User.GetUserId(), request, cancellationToken);

        return result.Status switch
        {
            RequestNutritionistStatus.Created =>
                Created("/api/v1/patients/me/nutritionist-requests", new IdResponse(result.RequestId!.Value)),
            RequestNutritionistStatus.CannotRequestSelf =>
                Problem(statusCode: StatusCodes.Status400BadRequest, title: "Você não pode solicitar acompanhamento a si mesmo."),
            RequestNutritionistStatus.NutritionistNotFound =>
                Problem(statusCode: StatusCodes.Status404NotFound, title: "Nutricionista não encontrado."),
            RequestNutritionistStatus.AlreadyHasNutritionist =>
                Problem(statusCode: StatusCodes.Status409Conflict, title: "Você já está em acompanhamento. Encerre o atual antes de solicitar outro."),
            RequestNutritionistStatus.PendingRequestExists =>
                Problem(statusCode: StatusCodes.Status409Conflict, title: "Você já tem uma solicitação pendente."),
            RequestNutritionistStatus.PatientNotFound =>
                Problem(statusCode: StatusCodes.Status404NotFound, title: "Perfil de paciente não encontrado.")
        };
    }

    /// <summary>Lista as solicitações do paciente, da mais recente para a mais antiga.</summary>
    /// <remarks>Filtre por `status`: `Pending`, `Accepted`, `Rejected` ou `Cancelled`.</remarks>
    [HttpGet("nutritionist-requests")]
    [ProducesResponseType<PagedResponse<PatientRequestItem>>(StatusCodes.Status200OK)]
    [ProducesValidationProblem]
    public async Task<IActionResult> ListRequests([FromQuery] NutritionistRequestListRequest request, CancellationToken cancellationToken)
    {
        var response = await followUpQueries.ListPatientRequestsAsync(User.GetUserId(), request, cancellationToken);

        return Ok(response);
    }

    /// <summary>Cancela uma solicitação que ainda está pendente.</summary>
    /// <param name="id">O Id da solicitação.</param>
    [HttpPost("nutritionist-requests/{id:guid}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesProblem(StatusCodes.Status404NotFound)]
    [ProducesProblem(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        var result = await cancelRequestUseCase.ExecuteAsync(User.GetUserId(), id, cancellationToken);

        return result switch
        {
            CancelNutritionistRequestResult.Cancelled => NoContent(),
            CancelNutritionistRequestResult.NotPending =>
                Problem(statusCode: StatusCodes.Status409Conflict, title: "Esta solicitação não está mais pendente."),
            CancelNutritionistRequestResult.NotFound => Problem(statusCode: StatusCodes.Status404NotFound, title: "Solicitação não encontrada.")
        };
    }

    /// <summary>Devolve o acompanhamento ativo e o nutricionista dele.</summary>
    /// <remarks>O 404 ("Você não está em acompanhamento.") não é um erro de verdade: é o estado de quem ainda não tem nutricionista.</remarks>
    [HttpGet("care-relationship")]
    [ProducesResponseType<MyCareRelationshipResponse>(StatusCodes.Status200OK)]
    [ProducesProblem(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCareRelationship(CancellationToken cancellationToken)
    {
        var response = await getMyCareRelationshipUseCase.ExecuteAsync(User.GetUserId(), cancellationToken);

        return response is null
            ? Problem(statusCode: StatusCodes.Status404NotFound, title: "Você não está em acompanhamento.")
            : Ok(response);
    }

    /// <summary>Encerra o acompanhamento ativo.</summary>
    /// <remarks>
    /// O nutricionista recebe um e-mail e perde o acesso aos dados do paciente. O plano ativo é finalizado, e os
    /// rascunhos são cancelados; o paciente continua vendo os planos finalizados.
    /// </remarks>
    [HttpPost("care-relationship/end")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesProblem(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> EndCareRelationship(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var ended = await endCareRelationshipUseCase.ExecuteAsync(userId, userId, cancellationToken);

        return ended
            ? NoContent()
            : Problem(statusCode: StatusCodes.Status404NotFound, title: "Você não está em acompanhamento.");
    }
}
