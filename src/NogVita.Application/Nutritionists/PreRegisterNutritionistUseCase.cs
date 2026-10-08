using Microsoft.Extensions.Logging;
using NogVita.Application.Abstractions;
using NogVita.Application.Common;
using NogVita.Domain.Invitations;
using NogVita.Domain.Users;

namespace NogVita.Application.Nutritionists;

public enum PreRegisterNutritionistStatus
{
    Created,
    Conflict
}

public sealed record PreRegisterNutritionistResult(
    PreRegisterNutritionistStatus Status,
    Guid? UserId = null,
    bool InvitationEmailSent = false)
{
    public static PreRegisterNutritionistResult Conflict() => new(PreRegisterNutritionistStatus.Conflict);
}

public sealed class PreRegisterNutritionistUseCase(
    IUserRepository userRepository,
    IInvitationRepository invitationRepository,
    ISecureTokenService secureTokenService,
    IEmailSender emailSender,
    IUnitOfWork unitOfWork,
    FrontendSettings frontendSettings,
    TimeProvider timeProvider,
    ILogger<PreRegisterNutritionistUseCase> logger)
{
    public static readonly TimeSpan InvitationLifetime = TimeSpan.FromHours(72);

    public async Task<PreRegisterNutritionistResult> ExecuteAsync(PreRegisterNutritionistRequest request, CancellationToken cancellationToken = default)
    {
        var cpf = Cpf.Create(request.Cpf);

        if (await userRepository.ExistsByCrnAsync(request.CrnRegion, request.CrnNumber, cancellationToken))
            return PreRegisterNutritionistResult.Conflict();

        var userByCpf = await userRepository.GetByCpfAsync(cpf, cancellationToken);
        var userByEmail = await userRepository.GetByEmailAsync(request.Email, cancellationToken);

        User user;
        bool isNewUser;

        if (userByCpf is null && userByEmail is null)
        {
            user = new User(request.Name, request.Email, cpf);
            userRepository.Add(user);
            isNewUser = true;
        }
        else if (userByCpf is not null && userByCpf.Id == userByEmail?.Id)
        {
            if (userByCpf.NutritionistProfile is not null)
                return PreRegisterNutritionistResult.Conflict();

            user = userByCpf;
            isNewUser = false;
        }
        else
        {
            return PreRegisterNutritionistResult.Conflict();
        }

        user.CreateNutritionistProfile(request.CrnRegion, request.CrnNumber);

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var token = secureTokenService.GenerateToken();
        var invitation = new NutritionistInvitation(user.Id, secureTokenService.Hash(token), now.Add(InvitationLifetime), now);
        invitationRepository.Add(invitation);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var emailSent = await TrySendInvitationAsync(user, token, isNewUser, cancellationToken);

        return new PreRegisterNutritionistResult(PreRegisterNutritionistStatus.Created, user.Id, emailSent);
    }

    private async Task<bool> TrySendInvitationAsync(User user, string token, bool isNewUser, CancellationToken cancellationToken)
    {
        var link = $"{frontendSettings.BaseUrl.TrimEnd('/')}/nutri/convite#token={token}";

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