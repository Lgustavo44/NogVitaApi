using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NogVita.Api.Extensions;
using NogVita.Api.OpenApi;
using NogVita.Application.Admin;
using NogVita.Application.Common.Pagination;
using NogVita.Application.Nutritionists;
using NogVita.Domain.Users;

namespace NogVita.Api.Controllers;

/// <summary>Os nutricionistas, pelo administrador: pré-cadastro, convite e cancelamento.</summary>
/// <remarks>Nutricionistas não se cadastram sozinhos: o administrador faz o pré-cadastro, e a pessoa aceita o convite por e-mail.</remarks>
[ApiController]
[Route("api/v1/admin/nutritionists")]
[Authorize(Roles = Roles.Admin)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
[ProducesProblem(StatusCodes.Status500InternalServerError)]
public sealed class AdminNutritionistsController(
    IAdminUserQueries adminUserQueries,
    PreRegisterNutritionistUseCase preRegisterNutritionistUseCase,
    ResendNutritionistInvitationUseCase resendNutritionistInvitationUseCase,
    CancelNutritionistPreRegistrationUseCase cancelPreRegistrationUseCase) : ControllerBase
{
    /// <summary>Lista os nutricionistas, do mais novo para o mais antigo.</summary>
    /// <remarks>`isActive` é a situação da conta; `isProfileActive: false` é um convite ainda não aceito.</remarks>
    [HttpGet]
    [ProducesResponseType<PagedResponse<AdminNutritionistSummary>>(StatusCodes.Status200OK)]
    [ProducesValidationProblem]
    public async Task<IActionResult> List([FromQuery] AdminUserListRequest request, CancellationToken cancellationToken)
    {
        var response = await adminUserQueries.ListNutritionistsAsync(request, cancellationToken);

        return Ok(response);
    }

    /// <summary>Devolve os detalhes de um nutricionista.</summary>
    /// <param name="id">O Id do usuário.</param>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<AdminUserDetail>(StatusCodes.Status200OK)]
    [ProducesProblem(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var user = await adminUserQueries.GetUserAsync(id, cancellationToken);

        if (user?.Nutritionist is null)
        {
            return Problem(statusCode: StatusCodes.Status404NotFound, title: "Nutricionista não encontrado.");
        }

        return Ok(user);
    }

    /// <summary>Pré-cadastra um nutricionista e envia o convite por e-mail.</summary>
    /// <remarks>
    /// Se o e-mail e o CPF já forem de um paciente, a mesma conta ganha o papel de nutricionista. O 409 tem uma mensagem
    /// genérica de propósito (CRN já usado, já é nutricionista, ou e-mail e CPF de pessoas diferentes). Se
    /// `invitationEmailSent` vier `false`, o cadastro foi feito e o e-mail falhou: reenvie o convite.
    /// </remarks>
    [HttpPost]
    [ProducesResponseType<PreRegisterNutritionistResponse>(StatusCodes.Status201Created)]
    [ProducesValidationProblem]
    [ProducesProblem(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> PreRegister(PreRegisterNutritionistRequest request, CancellationToken cancellationToken)
    {
        var result = await preRegisterNutritionistUseCase.ExecuteAsync(User.GetUserId(), request, cancellationToken);

        if (result.Status == PreRegisterNutritionistStatus.Conflict)
        {
            return Problem(statusCode: StatusCodes.Status409Conflict, title: "Não foi possível pré-cadastrar o nutricionista com esses dados.");
        }

        var response = new PreRegisterNutritionistResponse(result.UserId!.Value, result.InvitationEmailSent);

        return Created($"/api/v1/admin/nutritionists/{response.UserId}", response);
    }

    /// <summary>Gera um convite novo e invalida os anteriores.</summary>
    /// <param name="id">O Id do usuário.</param>
    [HttpPost("{id:guid}/invitation")]
    [ProducesResponseType<InvitationEmailResponse>(StatusCodes.Status200OK)]
    [ProducesProblem(StatusCodes.Status404NotFound)]
    [ProducesProblem(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ResendInvitation(Guid id, CancellationToken cancellationToken)
    {
        var status = await resendNutritionistInvitationUseCase.ExecuteAsync(User.GetUserId(), id, cancellationToken);

        return status switch
        {
            ResendInvitationStatus.Sent => Ok(new InvitationEmailResponse(InvitationEmailSent: true)),
            ResendInvitationStatus.EmailFailed => Ok(new InvitationEmailResponse(InvitationEmailSent: false)),
            ResendInvitationStatus.NotFound => Problem(statusCode: StatusCodes.Status404NotFound, title: "Nutricionista não encontrado."),
            ResendInvitationStatus.AlreadyActive => Problem(statusCode: StatusCodes.Status409Conflict, title: "O nutricionista já aceitou o convite."),
        };
    }

    /// <summary>Cancela um pré-cadastro cujo convite nunca foi aceito.</summary>
    /// <remarks>
    /// Se a conta foi criada só pelo pré-cadastro, ela é apagada, e o CPF, o e-mail e o CRN ficam livres. Se a pessoa já
    /// era paciente, só o perfil de nutricionista e o convite saem. O cancelamento fica na trilha de auditoria.
    /// </remarks>
    /// <param name="id">O Id do usuário.</param>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesProblem(StatusCodes.Status404NotFound)]
    [ProducesProblem(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CancelPreRegistration(Guid id, CancellationToken cancellationToken)
    {
        var status = await cancelPreRegistrationUseCase.ExecuteAsync(User.GetUserId(), id, cancellationToken);

        return status switch
        {
            CancelPreRegistrationStatus.Cancelled => NoContent(),
            CancelPreRegistrationStatus.NotFound => Problem(statusCode: StatusCodes.Status404NotFound, title: "Nutricionista não encontrado."),
            CancelPreRegistrationStatus.AlreadyAccepted => Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "O nutricionista já aceitou o convite. Para tirar o acesso, desative a conta."),
        };
    }
}
