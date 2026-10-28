using NogVita.Application.Abstractions;

namespace NogVita.Application.Nutritionists;

public enum CancelPreRegistrationStatus
{
    Cancelled,
    NotFound,
    AlreadyAccepted
}

// O caso da "Carla": um convite nunca aceito prende o CPF, o e-mail e o CRN. Cancelar libera os três.
public sealed class CancelNutritionistPreRegistrationUseCase(
    IUserRepository userRepository,
    IInvitationRepository invitationRepository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<CancelPreRegistrationStatus> ExecuteAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);

        if (user?.NutritionistProfile is null)
            return CancelPreRegistrationStatus.NotFound;

        if (user.NutritionistProfile.IsActive)
            return CancelPreRegistrationStatus.AlreadyAccepted;

        if (user.IsOnlyPendingNutritionist)
        {
            // A conta só existe por causa do pré-cadastro: sai inteira, e o CPF e o e-mail ficam livres.
            await userRepository.RemoveAsync(user, cancellationToken);
        }
        else
        {
            // Já era paciente (ou já tem senha): a conta continua, só o perfil de nutricionista e o convite saem.
            await invitationRepository.RevokePendingForUserAsync(user.Id, timeProvider.GetUtcNow().UtcDateTime, cancellationToken);
            userRepository.RemoveNutritionistProfile(user.RemovePendingNutritionistProfile());
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return CancelPreRegistrationStatus.Cancelled;
    }
}
