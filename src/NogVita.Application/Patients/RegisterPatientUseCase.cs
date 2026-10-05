using NogVita.Application.Abstractions;
using NogVita.Application.Auth;
using NogVita.Domain.Users;

namespace NogVita.Application.Patients;

public sealed class RegisterPatientUseCase(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    TokenIssuer tokenIssuer,
    IUnitOfWork unitOfWork)
{
    public async Task<AuthResponse?> ExecuteAsync(RegisterPatientRequest request, CancellationToken cancellationToken = default)
    {
        var cpf = Cpf.Create(request.Cpf);

        if (await userRepository.ExistsByEmailAsync(request.Email, cancellationToken)
            || await userRepository.ExistsByCpfAsync(cpf, cancellationToken))
            return null;

        var user = new User(request.Name, request.Email, cpf);
        user.SetPasswordHash(passwordHasher.Hash(request.Password));
        user.Activate();
        user.CreatePatientProfile(request.BirthDate, request.BiologicalSex, request.HeightInCm, request.Goal);

        userRepository.Add(user);
        var response = tokenIssuer.Issue(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return response;
    }
}