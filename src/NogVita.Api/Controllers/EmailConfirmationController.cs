using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NogVita.Application.Patients;

namespace NogVita.Api.Controllers;

[ApiController]
[Route("api/v1/auth/email-confirmation")]
[AllowAnonymous]
[EnableRateLimiting("auth")]
public sealed class EmailConfirmationController(
    ConfirmEmailUseCase confirmEmailUseCase,
    ResendEmailConfirmationUseCase resendEmailConfirmationUseCase) : ControllerBase
{
    [HttpPost("confirm")]
    public async Task<IActionResult> Confirm(ConfirmEmailRequest request, CancellationToken cancellationToken)
    {
        var confirmed = await confirmEmailUseCase.ExecuteAsync(request, cancellationToken);

        return confirmed
            ? NoContent()
            : Problem(statusCode: StatusCodes.Status400BadRequest, title: "Link de confirmação inválido ou expirado.");
    }

    [HttpPost("resend")]
    public async Task<IActionResult> Resend(ResendEmailConfirmationRequest request, CancellationToken cancellationToken)
    {
        await resendEmailConfirmationUseCase.ExecuteAsync(request, cancellationToken);

        return Accepted(new { message = "Se houver um cadastro pendente com este e-mail, enviamos um novo link de confirmação." });
    }
}