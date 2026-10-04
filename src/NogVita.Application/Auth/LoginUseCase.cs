using NogVita.Application.Abstractions;

namespace NogVita.Application.Auth;

public sealed class LoginUseCase(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    TokenIssuer tokenIssuer,
    IUnitOfWork unitOfWork)
{
    public async Task<AuthResponse?> ExecuteAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByEmailAsync(request.Email, cancellationToken);

        if (user is null)
            return null;

        if (!user.IsActive)
            return null;

        if (user.PasswordHash is null)
            return null;

        if (!passwordHasher.Verify(user.PasswordHash, request.Password))
            return null;

        var response = tokenIssuer.Issue(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return response;
    }
}