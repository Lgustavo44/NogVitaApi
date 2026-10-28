using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NogVita.Api.OpenApi;
using NogVita.Application.Common;
using NogVita.Application.Patients;

namespace NogVita.Api.Controllers;

/// <summary>Confirmação do e-mail do cadastro.</summary>
[ApiController]
[Route("api/v1/auth/email-confirmation")]
[AllowAnonymous]
[EnableRateLimiting("auth")]
[ProducesResponseType(StatusCodes.Status429TooManyRequests)]
[ProducesProblem(StatusCodes.Status500InternalServerError)]
public sealed class EmailConfirmationController(
    ConfirmEmailUseCase confirmEmailUseCase,
    ResendEmailConfirmationUseCase resendEmailConfirmationUseCase) : ControllerBase
{
    /// <summary>Confirma o e-mail com o token do link.</summary>
    /// <remarks>
    /// O link do e-mail leva o token no fragmento (`/confirmar-email#token=...`). O front lê o token e o envia aqui.
    /// O link vale 24 horas e só pode ser usado uma vez. Depois do 204, a pessoa já pode entrar; um token inválido ou expirado responde 400.
    /// </remarks>
    [HttpPost("confirm")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesValidationProblem]
    [ProducesProblem(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Confirm(ConfirmEmailRequest request, CancellationToken cancellationToken)
    {
        var confirmed = await confirmEmailUseCase.ExecuteAsync(request, cancellationToken);

        return confirmed
            ? NoContent()
            : Problem(statusCode: StatusCodes.Status400BadRequest, title: "Link de confirmação inválido ou expirado.");
    }

    /// <summary>Envia um novo link de confirmação.</summary>
    /// <remarks>A resposta é sempre a mesma (202), exista ou não um cadastro pendente com esse e-mail. O link anterior deixa de valer.</remarks>
    [HttpPost("resend")]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status202Accepted)]
    [ProducesValidationProblem]
    public async Task<IActionResult> Resend(ResendEmailConfirmationRequest request, CancellationToken cancellationToken)
    {
        await resendEmailConfirmationUseCase.ExecuteAsync(request, cancellationToken);

        return Accepted(new MessageResponse("Se houver um cadastro pendente com este e-mail, enviamos um novo link de confirmação."));
    }
}
