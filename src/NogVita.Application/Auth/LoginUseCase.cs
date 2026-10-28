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

        // Sempre verifica a senha, mesmo sem usuário: o tempo de resposta não revela se o e-mail existe.
        var check = passwordHasher.Verify(user?.PasswordHash, request.Password);

        if (user is null || check == PasswordCheck.Failed)
        {
            return InvalidCredentials;
        }

        if (!user.IsEmailConfirmed)
        {
            return new LoginResult(LoginStatus.EmailNotConfirmed);
        }

        if (!user.IsActive)
        {
            return InvalidCredentials;
        }

        // Hash com parâmetros antigos: aproveita a senha em texto, que só existe agora, para gerar um novo.
        if (check == PasswordCheck.SuccessRehashNeeded)
        {
            user.SetPasswordHash(passwordHasher.Hash(request.Password));
        }

        var tokens = tokenIssuer.Issue(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new LoginResult(LoginStatus.Succeeded, tokens);
    }
}
