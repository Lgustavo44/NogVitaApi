using NogVita.Application.Abstractions;

namespace NogVita.Application.Patients;

public sealed class ConfirmEmailUseCase(
    IEmailConfirmationTokenRepository tokenRepository,
    IUserRepository userRepository,
    ISecureTokenService secureTokenService,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<bool> ExecuteAsync(ConfirmEmailRequest request, CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;

        var tokenHash = secureTokenService.Hash(request.Token);
        var token = await tokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);
        if (token == null || !token.IsValid(now))
        {
            return false;
        }
        var user = await userRepository.GetByIdAsync(token.UserId, cancellationToken);
        if (user == null)
        {
            return false;
        }
        token.MarkAsUsed(now);
        user.ConfirmEmail(now);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}