using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NogVita.Api.Extensions;
using NogVita.Api.OpenApi;
using NogVita.Application.Admin;
using NogVita.Application.Common.Pagination;
using NogVita.Domain.Users;

namespace NogVita.Api.Controllers;

/// <summary>Todos os usuários, pelo administrador: a lista, os detalhes e a ativação das contas.</summary>
[ApiController]
[Route("api/v1/admin/users")]
[Authorize(Roles = Roles.Admin)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
[ProducesProblem(StatusCodes.Status500InternalServerError)]
public sealed class AdminUsersController(IAdminUserQueries adminUserQueries, DeactivateUserUseCase deactivateUserUseCase, ActivateUserUseCase activateUserUseCase) : ControllerBase
{
    /// <summary>Lista todos os usuários, do mais novo para o mais antigo.</summary>
    /// <remarks>`search` busca no nome ou no e-mail; `isActive` filtra pela situação da conta.</remarks>
    [HttpGet]
    [ProducesResponseType<PagedResponse<AdminUserSummary>>(StatusCodes.Status200OK)]
    [ProducesValidationProblem]
    public async Task<IActionResult> List([FromQuery] AdminUserListRequest request, CancellationToken cancellationToken)
    {
        var response = await adminUserQueries.ListUsersAsync(request, cancellationToken);

        return Ok(response);
    }

    /// <summary>Devolve os detalhes de um usuário, com os perfis de paciente e de nutricionista.</summary>
    /// <param name="id">O Id do usuário.</param>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<AdminUserDetail>(StatusCodes.Status200OK)]
    [ProducesProblem(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var user = await adminUserQueries.GetUserAsync(id, cancellationToken);

        if (user is null)
        {
            return Problem(statusCode: StatusCodes.Status404NotFound, title: "Usuário não encontrado.");
        }

        return Ok(user);
    }

    /// <summary>Desativa uma conta e encerra todas as sessões dela.</summary>
    /// <remarks>
    /// O bloqueio é explícito: confirmar o e-mail, redefinir a senha ou aceitar um convite não reativa a conta.
    /// Desativar um nutricionista também encerra os acompanhamentos dele (os pacientes são avisados por e-mail),
    /// finaliza ou cancela os planos em aberto e recusa as solicitações pendentes. Desativar um paciente o tira do
    /// alcance do nutricionista enquanto durar o bloqueio. O admin não pode desativar a própria conta (409).
    /// </remarks>
    /// <param name="id">O Id do usuário.</param>
    [HttpPost("{id:guid}/deactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesProblem(StatusCodes.Status404NotFound)]
    [ProducesProblem(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        var result = await deactivateUserUseCase.ExecuteAsync(User.GetUserId(), id, cancellationToken);

        return result switch
        {
            DeactivateUserResult.Success => NoContent(),
            DeactivateUserResult.NotFound => Problem(statusCode: StatusCodes.Status404NotFound, title: "Usuário não encontrado."),
            DeactivateUserResult.CannotDeactivateSelf => Problem(statusCode: StatusCodes.Status409Conflict, title: "Você não pode desativar a sua própria conta."),
        };
    }

    /// <summary>Reativa uma conta.</summary>
    /// <remarks>
    /// Um nutricionista que ainda não aceitou o convite não tem senha (400): reenvie o convite. Reativar um nutricionista
    /// não recria os acompanhamentos encerrados na desativação.
    /// </remarks>
    /// <param name="id">O Id do usuário.</param>
    [HttpPost("{id:guid}/activate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesProblem(StatusCodes.Status400BadRequest)]
    [ProducesProblem(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        var result = await activateUserUseCase.ExecuteAsync(User.GetUserId(), id, cancellationToken);
        return result switch
        {
            ActivateUserResult.Success => NoContent(),
            ActivateUserResult.NotFound => Problem(statusCode: StatusCodes.Status404NotFound, title: "Usuário não encontrado."),
            ActivateUserResult.PasswordNotDefined => Problem(statusCode: StatusCodes.Status400BadRequest, title: "Senha não definida."),
        };
    }
}
