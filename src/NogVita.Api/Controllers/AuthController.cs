using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NogVita.Api.Extensions;
using NogVita.Api.OpenApi;
using NogVita.Application.Auth;
using NogVita.Application.Common;
using NogVita.Application.Patients;

namespace NogVita.Api.Controllers;

/// <summary>Login, sessão e cadastro de paciente.</summary>
[ApiController]
[EnableRateLimiting("auth")]
[Route("api/v1/auth")]
[ProducesResponseType(StatusCodes.Status429TooManyRequests)]
[ProducesProblem(StatusCodes.Status500InternalServerError)]
public sealed class AuthController(
    LoginUseCase loginUseCase,
    RefreshTokenUseCase refreshTokenUseCase,
    LogoutUseCase logoutUseCase,
    RegisterPatientUseCase registerPatientUseCase
    ) : ControllerBase
{
    /// <summary>Entra com e-mail e senha.</summary>
    /// <remarks>
    /// Devolve o token de acesso (15 minutos) e o refresh token. E-mail inexistente e senha errada dão a mesma resposta,
    /// para não revelar quem tem conta. Quem ainda não confirmou o e-mail recebe 403, e pode pedir um novo link.
    /// </remarks>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesValidationProblem]
    [ProducesProblem(StatusCodes.Status401Unauthorized)]
    [ProducesProblem(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await loginUseCase.ExecuteAsync(request, cancellationToken);

        return result.Status switch
        {
            LoginStatus.Succeeded => Ok(result.Tokens),
            LoginStatus.EmailNotConfirmed => Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Confirme seu e-mail antes de entrar."),
            LoginStatus.InvalidCredentials => Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "E-mail ou senha inválidos.")
        };
    }

    /// <summary>Diz quem é o usuário do token e quais papéis ele tem.</summary>
    /// <remarks>Use depois do login para escolher a área do app: paciente, nutricionista ou administração.</remarks>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType<MeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<MeResponse> Me() =>
        new MeResponse(User.GetUserId(), User.FindAll("role").Select(claim => claim.Value).ToList());

    /// <summary>Troca o refresh token por um par novo.</summary>
    /// <remarks>
    /// Cada refresh token vale uma vez só. Reusar um token já trocado é tratado como roubo: a API recusa e encerra
    /// também a sessão nova. Por isso, renove uma vez por vez, e guarde sempre o par que voltou.
    /// </remarks>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesValidationProblem]
    [ProducesProblem(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh(RefreshRequest request, CancellationToken cancellationToken)
    {
        var response = await refreshTokenUseCase.ExecuteAsync(request, cancellationToken);

        if (response is null)
            return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Sessão inválida ou expirada.");

        return Ok(response);
    }

    /// <summary>Encerra a sessão do refresh token informado.</summary>
    /// <remarks>Responde 204 mesmo que o token já não valha: sair duas vezes não é erro.</remarks>
    [HttpPost("logout")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesValidationProblem]
    public async Task<IActionResult> Logout(LogoutRequest request, CancellationToken cancellationToken)
    {
        await logoutUseCase.ExecuteAsync(request, cancellationToken);

        return NoContent();
    }

    /// <summary>Cadastra um paciente e envia o e-mail de confirmação.</summary>
    /// <remarks>
    /// A resposta é sempre a mesma (202), exista ou não uma conta com esse e-mail ou CPF: o cadastro nunca revela
    /// quem já tem conta. Quem já tem recebe um aviso por e-mail. A conta só entra depois de confirmar o e-mail.
    /// A senha precisa ter pelo menos 15 caracteres, e o paciente, pelo menos 18 anos.
    /// </remarks>
    [HttpPost("register/patient")]
    [AllowAnonymous]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status202Accepted)]
    [ProducesValidationProblem]
    public async Task<IActionResult> RegisterPatient(RegisterPatientRequest request, CancellationToken cancellationToken)
    {
        await registerPatientUseCase.ExecuteAsync(request, cancellationToken);

        return Accepted(new MessageResponse("Se os dados estiverem corretos, enviamos um e-mail para confirmar o cadastro."));
    }
}
