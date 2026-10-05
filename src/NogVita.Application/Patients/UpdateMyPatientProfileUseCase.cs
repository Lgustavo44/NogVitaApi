using NogVita.Application.Abstractions;

namespace NogVita.Application.Patients;

public sealed class UpdateMyPatientProfileUseCase(
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<PatientProfileResponse?> ExecuteAsync(Guid userId, UpdatePatientProfileRequest request, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);

        if (user is null || !user.IsActive || user.PatientProfile is null)
            return null;
        if (user?.PatientProfile is null)
            return null;

        user.UpdatePatientProfile(request.BirthDate, request.BiologicalSex, request.HeightInCm, request.Goal);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        return PatientProfileResponse.From(user, user.PatientProfile, today);
    }
}