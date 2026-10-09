using Microsoft.Extensions.Logging;
using NogVita.Application.Abstractions;
using NogVita.Application.Common;
using NogVita.Domain.Auth;
using NogVita.Domain.Users;

namespace NogVita.Application.Patients;

public sealed class EmailConfirmationService(
    IEmailConfirmationTokenRepository tokenRepository,
    ISecureTokenService secureTokenService,
    IEmailSender emailSender,
    FrontendSettings frontendSettings,
    TimeProvider timeProvider,
    ILogger<EmailConfirmationService> logger)
{
    public static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(24);

    public string CreateToken(User user)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var token = secureTokenService.GenerateToken();

        tokenRepository.Add(new EmailConfirmationToken(user.Id, secureTokenService.Hash(token), now.Add(TokenLifetime), now));

        return token;
    }

    public async Task<string> RenewTokenAsync(User user, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        await tokenRepository.RevokePendingForUserAsync(user.Id, now, cancellationToken);

        return CreateToken(user);
    }

    public Task<bool> TrySendConfirmationAsync(User user, string token, CancellationToken cancellationToken)
    {
        var link = $"{frontendSettings.BaseUrl.TrimEnd('/')}/confirmar-email#token={token}";

        return TrySendAsync(ConfirmationEmail.Create(user.Email, user.Name, link), user.Id, cancellationToken);
    }

    public Task<bool> TrySendAlreadyRegisteredAsync(User user, CancellationToken cancellationToken)
    {
        return TrySendAsync(AlreadyRegisteredEmail.Create(user.Email, user.Name), user.Id, cancellationToken);
    }

    private async Task<bool> TrySendAsync(EmailMessage message, Guid userId, CancellationToken cancellationToken)
    {
        try
        {
            await emailSender.SendAsync(message, cancellationToken);
            return true;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Falha ao enviar e-mail de confirmação para o usuário {UserId}.", userId);
            return false;
        }
    }
}