using Microsoft.Extensions.Logging;
using NogVita.Application.Abstractions;
using NogVita.Domain.Users;

namespace NogVita.Application.FollowUps;

public sealed class FollowUpNotificationService(
    IEmailSender emailSender,
    ILogger<FollowUpNotificationService> logger)
{
    public async Task<bool> TrySendEndedAsync(User recipient, User endedBy, CancellationToken cancellationToken)
    {
        try
        {
            await emailSender.SendAsync(
                CareRelationshipEndedEmail.Create(recipient.Email, recipient.Name, endedBy.Name),
                cancellationToken);
            return true;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Falha ao enviar e-mail de encerramento para o usuário {UserId}.", recipient.Id);
            return false;
        }
    }
}