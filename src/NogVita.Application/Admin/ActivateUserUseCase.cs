using NogVita.Application.Abstractions;
using NogVita.Domain.Audit;

namespace NogVita.Application.Admin;

public enum ActivateUserResult
{
    Success,
    NotFound,
    PasswordNotDefined
}

public sealed class ActivateUserUseCase(IUserRepository userRepository, IUnitOfWork unitOfWork, AdminAuditLog auditLog)
{
    public async Task<ActivateUserResult> ExecuteAsync(Guid adminId, Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);

        if (user is null)
            return ActivateUserResult.NotFound;
        if (user.PasswordHash is null)
            return ActivateUserResult.PasswordNotDefined;

        user.Activate();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        auditLog.Record(adminId, AdminAuditAction.UserActivated, user.Id);
        return ActivateUserResult.Success;
    }
}
