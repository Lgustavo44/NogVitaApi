using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NogVita.Application.Auth;

namespace NogVita.Api.Controllers;

[ApiController]
[Route("api/v1/auth/password-reset")]
[AllowAnonymous]
[EnableRateLimiting("auth")]
public sealed class PasswordResetController(
    RequestPasswordResetUseCase requestPasswordResetUseCase,
    ConfirmPasswordResetUseCase confirmPasswordResetUseCase) : ControllerBase
{
    [HttpPost("request")]
    public async Task<IActionResult> RequestReset(RequestPasswordResetRequest request, CancellationToken cancellationToken)
    {
        await requestPasswordResetUseCase.ExecuteAsync(request, cancellationToken);

        return Accepted(new { message = "Se houver uma conta com este e-mail, enviamos um link para redefinir a senha." });
    }

    [HttpPost("confirm")]
    public async Task<IActionResult> Confirm(ConfirmPasswordResetRequest request, CancellationToken cancellationToken)
    {
        var changed = await confirmPasswordResetUseCase.ExecuteAsync(request, cancellationToken);

        return changed
            ? NoContent()
            : Problem(statusCode: StatusCodes.Status400BadRequest, title: "Link de redefinição inválido ou expirado.");
    }
}