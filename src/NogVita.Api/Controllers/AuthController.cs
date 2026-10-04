using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NogVita.Application.Auth;

namespace NogVita.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(
    LoginUseCase loginUseCase,
    RefreshTokenUseCase refreshTokenUseCase,
    LogoutUseCase logoutUseCase) : ControllerBase
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
}