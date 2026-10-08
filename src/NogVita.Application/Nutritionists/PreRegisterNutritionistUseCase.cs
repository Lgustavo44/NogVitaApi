using NogVita.Application.Abstractions;
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
    IUnitOfWork unitOfWork,
    NutritionistInvitationService invitationService)
{
    public async Task<PreRegisterNutritionistResult> ExecuteAsync(PreRegisterNutritionistRequest request, CancellationToken cancellationToken = default)
    {
        var cpf = Cpf.Create(request.Cpf);

        if (await userRepository.ExistsByCrnAsync(request.CrnRegion, request.CrnNumber, cancellationToken))
            return PreRegisterNutritionistResult.Conflict();

        var userByCpf = await userRepository.GetByCpfAsync(cpf, cancellationToken);
        var userByEmail = await userRepository.GetByEmailAsync(request.Email, cancellationToken);

        User user;

        if (userByCpf is null && userByEmail is null)
        {
            user = new User(request.Name, request.Email, cpf);
            userRepository.Add(user);
        }
        else if (userByCpf is not null && userByCpf.Id == userByEmail?.Id)
        {
            if (userByCpf.NutritionistProfile is not null)
                return PreRegisterNutritionistResult.Conflict();

            user = userByCpf;
        }
        else
        {
            return PreRegisterNutritionistResult.Conflict();
        }

        user.CreateNutritionistProfile(request.CrnRegion, request.CrnNumber);

        var token = invitationService.CreateInvitation(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var emailSent = await invitationService.TrySendAsync(user, token, cancellationToken);

        return new PreRegisterNutritionistResult(PreRegisterNutritionistStatus.Created, user.Id, emailSent);
    }
}