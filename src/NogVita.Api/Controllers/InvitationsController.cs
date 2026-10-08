using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NogVita.Application.Nutritionists;

namespace NogVita.Api.Controllers;

[ApiController]
[Route("api/v1/auth/invitations")]
public sealed class InvitationsController(AcceptNutritionistInvitationUseCase acceptNutritionistInvitationUseCase) : ControllerBase
{
    [HttpPost("accept")]
    [AllowAnonymous]
    public async Task<IActionResult> Accept(AcceptInvitationRequest request, CancellationToken cancellationToken)
    {
        var result = await acceptNutritionistInvitationUseCase.ExecuteAsync(request, cancellationToken);

        return result switch
        {
            AcceptInvitationResult.Accepted => NoContent(),
            AcceptInvitationResult.InvalidInvitation => Problem(statusCode: StatusCodes.Status400BadRequest, title: "Convite inválido ou expirado."),
            AcceptInvitationResult.PasswordRequired => Problem(statusCode: StatusCodes.Status400BadRequest, title: "Defina uma senha para ativar sua conta."),
            _ => throw new InvalidOperationException($"Resultado não tratado: {result}")
        };
    }
}