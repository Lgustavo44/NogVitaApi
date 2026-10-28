using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NogVita.Api.OpenApi;
using NogVita.Application.Auth;
using NogVita.Application.Common;

namespace NogVita.Api.Controllers;

/// <summary>Recuperação de senha ("esqueci minha senha").</summary>
[ApiController]
[Route("api/v1/auth/password-reset")]
[AllowAnonymous]
[EnableRateLimiting("auth")]
[ProducesResponseType(StatusCodes.Status429TooManyRequests)]
[ProducesProblem(StatusCodes.Status500InternalServerError)]
public sealed class PasswordResetController(
    RequestPasswordResetUseCase requestPasswordResetUseCase,
    ConfirmPasswordResetUseCase confirmPasswordResetUseCase) : ControllerBase
{
    /// <summary>Envia o link para redefinir a senha.</summary>
    /// <remarks>
    /// A resposta é sempre a mesma (202), exista ou não uma conta com esse e-mail. O link vale 1 hora, é de uso único,
    /// e um pedido novo invalida o anterior. Contas desativadas pelo admin não recebem o link.
    /// </remarks>
    [HttpPost("request")]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status202Accepted)]
    [ProducesValidationProblem]
    public async Task<IActionResult> RequestReset(RequestPasswordResetRequest request, CancellationToken cancellationToken)
    {
        await requestPasswordResetUseCase.ExecuteAsync(request, cancellationToken);

        return Accepted(new MessageResponse("Se houver uma conta com este e-mail, enviamos um link para redefinir a senha."));
    }

    /// <summary>Define a nova senha com o token do link.</summary>
    /// <remarks>
    /// Depois da troca, todas as sessões abertas são encerradas (inclusive a de quem tenha invadido a conta) e o dono
    /// recebe um aviso por e-mail. A nova senha segue a mesma política do cadastro: pelo menos 15 caracteres. Um token inválido ou expirado responde 400.
    /// </remarks>
    [HttpPost("confirm")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesValidationProblem]
    [ProducesProblem(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Confirm(ConfirmPasswordResetRequest request, CancellationToken cancellationToken)
    {
        var changed = await confirmPasswordResetUseCase.ExecuteAsync(request, cancellationToken);

        return changed
            ? NoContent()
            : Problem(statusCode: StatusCodes.Status400BadRequest, title: "Link de redefinição inválido ou expirado.");
    }
}
