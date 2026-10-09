using NogVita.Application.Abstractions;
using NogVita.Application.Auth;
using NogVita.Domain.Users;

namespace NogVita.Application.Patients;

public sealed class RegisterPatientUseCase(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    EmailConfirmationService emailConfirmationService,
    IUnitOfWork unitOfWork)
{
    public async Task ExecuteAsync(RegisterPatientRequest request, CancellationToken cancellationToken = default)
    {
        var cpf = Cpf.Create(request.Cpf);

        var existingUser = await userRepository.GetByEmailAsync(request.Email, cancellationToken);

        if (existingUser is not null)
        {
            if (existingUser.PasswordHash is not null && !existingUser.IsEmailConfirmed)
            {
                var renewedToken = await emailConfirmationService.RenewTokenAsync(existingUser, cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                await emailConfirmationService.TrySendConfirmationAsync(existingUser, renewedToken, cancellationToken);
                return;
            }

            await emailConfirmationService.TrySendAlreadyRegisteredAsync(existingUser, cancellationToken);
            return;
        }

        var existingCpf = await userRepository.GetByCpfAsync(cpf, cancellationToken);

        if (existingCpf != null)
        {
            return;
        }


        var user = new User(request.Name, request.Email, cpf);
        user.SetPasswordHash(passwordHasher.Hash(request.Password));
        user.CreatePatientProfile(request.BirthDate, request.BiologicalSex, request.HeightInCm, request.Goal);

        userRepository.Add(user);

        var token = emailConfirmationService.CreateToken(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await emailConfirmationService.TrySendConfirmationAsync(user, token, cancellationToken);
    }
}