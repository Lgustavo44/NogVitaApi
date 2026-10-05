using NogVita.Application.Abstractions;

namespace NogVita.Application.Admin;

public enum ActivateUserResult
{
    Success,
    NotFound,
    PasswordNotDefined
}

public sealed class ActivateUserUseCase(IUserRepository userRepository, IUnitOfWork unitOfWork)
{
    public async Task<ActivateUserResult> ExecuteAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);

        if (user is null)
            return ActivateUserResult.NotFound;
        if (user.PasswordHash is null)
            return ActivateUserResult.PasswordNotDefined;

        user.Activate();
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ActivateUserResult.Success;
    }
}