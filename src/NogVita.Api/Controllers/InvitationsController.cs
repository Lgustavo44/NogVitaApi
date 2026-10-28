using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NogVita.Api.OpenApi;
using NogVita.Application.Nutritionists;

namespace NogVita.Api.Controllers;

/// <summary>Convite de nutricionista.</summary>
[ApiController]
[EnableRateLimiting("auth")]
[Route("api/v1/auth/invitations")]
[ProducesResponseType(StatusCodes.Status429TooManyRequests)]
[ProducesProblem(StatusCodes.Status500InternalServerError)]
public sealed class InvitationsController(AcceptNutritionistInvitationUseCase acceptNutritionistInvitationUseCase) : ControllerBase
{
    /// <summary>Aceita o convite de nutricionista enviado pelo administrador.</summary>
    /// <remarks>
    /// Quem ainda não tem conta precisa enviar a senha (pelo menos 15 caracteres); sem ela, a resposta é 400 com
    /// "Defina uma senha para ativar sua conta.". Quem já é paciente envia `password: null` e continua com a senha atual.
    /// O convite vale 72 horas e só pode ser usado uma vez; um convite inválido ou expirado responde 400.
    /// </remarks>
    [HttpPost("accept")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesValidationProblem]
    [ProducesProblem(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Accept(AcceptInvitationRequest request, CancellationToken cancellationToken)
    {
        var result = await acceptNutritionistInvitationUseCase.ExecuteAsync(request, cancellationToken);

        return result switch
        {
            AcceptInvitationResult.Accepted => NoContent(),
            AcceptInvitationResult.InvalidInvitation => Problem(statusCode: StatusCodes.Status400BadRequest, title: "Convite inválido ou expirado."),
            AcceptInvitationResult.PasswordRequired => Problem(statusCode: StatusCodes.Status400BadRequest, title: "Defina uma senha para ativar sua conta."),
        };
    }
}
