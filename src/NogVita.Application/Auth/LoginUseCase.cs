using NogVita.Application.Abstractions;

namespace NogVita.Application.Auth;

public enum LoginStatus
{
    Succeeded,
    InvalidCredentials,
    EmailNotConfirmed
}

public sealed record LoginResult(LoginStatus Status, AuthResponse? Tokens = null);
public sealed class LoginUseCase(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    TokenIssuer tokenIssuer,
    IUnitOfWork unitOfWork)
{
    private static readonly LoginResult InvalidCredentials = new(LoginStatus.InvalidCredentials);

    public async Task<LoginResult> ExecuteAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByEmailAsync(request.Email, cancellationToken);

        if (user?.PasswordHash is null || !passwordHasher.Verify(user.PasswordHash, request.Password))
        {
            return new LoginResult(LoginStatus.InvalidCredentials);
        }

        if (!user.IsEmailConfirmed)
        {
            return new LoginResult(LoginStatus.EmailNotConfirmed);
        }

        if (!user.IsActive)
        {
            return new LoginResult(LoginStatus.InvalidCredentials);
        }

        var tokens = tokenIssuer.Issue(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new LoginResult(LoginStatus.Succeeded, tokens);
    }
}