using Microsoft.Extensions.Logging;
using NogVita.Application.Abstractions;
using NogVita.Application.Common;
using NogVita.Domain.Invitations;
using NogVita.Domain.Users;

namespace NogVita.Application.Nutritionists;

public sealed class NutritionistInvitationService(
    IInvitationRepository invitationRepository,
    ISecureTokenService secureTokenService,
    IEmailSender emailSender,
    FrontendSettings frontendSettings,
    TimeProvider timeProvider,
    ILogger<NutritionistInvitationService> logger)
{
    public static readonly TimeSpan InvitationLifetime = TimeSpan.FromHours(72);

    public string CreateInvitation(User user)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var token = secureTokenService.GenerateToken();

        invitationRepository.Add(new NutritionistInvitation(user.Id, secureTokenService.Hash(token), now.Add(InvitationLifetime), now));

        return token;
    }

    public async Task<bool> TrySendAsync(User user, string token, CancellationToken cancellationToken)
    {
        var link = $"{frontendSettings.BaseUrl.TrimEnd('/')}/nutri/convite#token={token}";
        var isNewUser = user.PasswordHash is null;

        try
        {
            await emailSender.SendAsync(InvitationEmail.Create(user.Email, user.Name, link, isNewUser), cancellationToken);
            return true;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Falha ao enviar o convite de nutricionista para o usuário {UserId}.", user.Id);
            return false;
        }
    }
}