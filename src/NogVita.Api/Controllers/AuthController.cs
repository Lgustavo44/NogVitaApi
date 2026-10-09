using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NogVita.Application.Auth;
using NogVita.Application.Patients;

namespace NogVita.Api.Controllers;

[ApiController]
[EnableRateLimiting("auth")]
[Route("api/v1/auth")]
public sealed class AuthController(
    LoginUseCase loginUseCase,
    RefreshTokenUseCase refreshTokenUseCase,
    LogoutUseCase logoutUseCase,
    RegisterPatientUseCase registerPatientUseCase
    ) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var response = await loginUseCase.ExecuteAsync(request, cancellationToken);

        if (response is null)
            return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "E-mail ou senha inválidos.");

        return Ok(response);
    }

    [HttpGet("me")]
    [Authorize]
    public IActionResult Me()
    {
        var userId = User.FindFirst("sub")?.Value;
        var roles = User.FindAll("role").Select(claim => claim.Value);

        return Ok(new { userId, roles });
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh(RefreshRequest request, CancellationToken cancellationToken)
    {
        var response = await refreshTokenUseCase.ExecuteAsync(request, cancellationToken);

        if (response is null)
            return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Sessão inválida ou expirada.");

        return Ok(response);
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout(LogoutRequest request, CancellationToken cancellationToken)
    {
        await logoutUseCase.ExecuteAsync(request, cancellationToken);

        return NoContent();
    }

    [HttpPost("register/patient")]
    [AllowAnonymous]
    public async Task<IActionResult> RegisterPatient(RegisterPatientRequest request, CancellationToken cancellationToken)
    {
        await registerPatientUseCase.ExecuteAsync(request, cancellationToken);

        return Accepted(new { message = "Se os dados estiverem corretos, enviamos um e-mail para confirmar o cadastro." });
    }
}