using NogVita.Application.Abstractions;
using NogVita.Application.Admin;
using NogVita.Domain.Audit;

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
    IAdminAuditRepository auditRepository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    AdminAuditLog auditLog)
{
    public async Task<CancelPreRegistrationStatus> ExecuteAsync(Guid adminId, Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);

        if (user?.NutritionistProfile is null)
            return CancelPreRegistrationStatus.NotFound;

        if (user.NutritionistProfile.IsActive)
            return CancelPreRegistrationStatus.AlreadyAccepted;

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var crn = $"CRN-{user.NutritionistProfile.CrnRegion} {user.NutritionistProfile.CrnNumber}";
        string details;

        if (user.IsOnlyPendingNutritionist)
        {
            // A conta só existe por causa do pré-cadastro: sai inteira, e o CPF e o e-mail ficam livres.
            await userRepository.RemoveAsync(user, cancellationToken);
            details = $"{crn}. Conta apagada.";
        }
        else
        {
            // Já era paciente (ou já tem senha): a conta continua, só o perfil de nutricionista e o convite saem.
            await invitationRepository.RevokePendingForUserAsync(user.Id, now, cancellationToken);
            userRepository.RemoveNutritionistProfile(user.RemovePendingNutritionistProfile());
            details = $"{crn}. Conta mantida, só o perfil de nutricionista foi removido.";
        }

        // Esta ação apaga dados: além do log (D173), fica um registro durável na tabela, sem o nome, o CPF ou o e-mail (D161).
        auditRepository.Add(new AdminAuditEntry(
            adminId, AdminAuditAction.NutritionistPreRegistrationCancelled, userId, details, now));

        await unitOfWork.SaveChangesAsync(cancellationToken);
        auditLog.Record(adminId, AdminAuditAction.NutritionistPreRegistrationCancelled, userId, details);

        return CancelPreRegistrationStatus.Cancelled;
    }
}
