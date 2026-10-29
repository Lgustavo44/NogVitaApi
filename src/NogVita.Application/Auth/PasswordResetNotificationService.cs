using Microsoft.Extensions.Logging;
using NogVita.Application.Abstractions;
using NogVita.Application.Common;
using NogVita.Domain.Users;

namespace NogVita.Application.Auth;

public sealed class PasswordResetNotificationService(
    IEmailSender emailSender,
    FrontendSettings frontendSettings,
    ILogger<PasswordResetNotificationService> logger)
{
    public Task<bool> TrySendResetLinkAsync(User user, string token, CancellationToken cancellationToken)
    {
        var link = $"{frontendSettings.BaseUrl.TrimEnd('/')}/redefinir-senha#token={token}";

        return TrySendAsync(PasswordResetEmail.Create(user.Email, user.Name, link), user.Id, cancellationToken);
    }

    public Task<bool> TrySendPasswordChangedAsync(User user, CancellationToken cancellationToken) =>
        TrySendAsync(PasswordChangedEmail.Create(user.Email, user.Name), user.Id, cancellationToken);

    private async Task<bool> TrySendAsync(EmailMessage message, Guid userId, CancellationToken cancellationToken)
    {
        try
        {
            await emailSender.SendAsync(message, cancellationToken);
            return true;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Falha ao enviar e-mail de senha para o usuário {UserId}.", userId);
            return false;
        }
    }
}
