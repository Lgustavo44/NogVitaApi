using NogVita.Application.Abstractions;

namespace NogVita.Application.Patients;

public sealed class ResendEmailConfirmationUseCase(
    IUserRepository userRepository,
    EmailConfirmationService emailConfirmationService,
    IUnitOfWork unitOfWork)
{
    public async Task ExecuteAsync(ResendEmailConfirmationRequest request, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (user == null || string.IsNullOrEmpty(user.PasswordHash) || user.IsEmailConfirmed)
        {
            return;
        }
        var token = await emailConfirmationService.RenewTokenAsync(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await emailConfirmationService.TrySendConfirmationAsync(user, token, cancellationToken);
    }
}