using NogVita.Application.Abstractions;
using NogVita.Application.Photos;

namespace NogVita.Application.Patients;

public sealed class UpdateMyPatientProfileUseCase(
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    IFileStorage fileStorage,
    TimeProvider timeProvider)
{
    public async Task<PatientProfileResponse?> ExecuteAsync(Guid userId, UpdatePatientProfileRequest request, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);

        if (user is null || !user.IsActive || user.PatientProfile is null)
        {
            return null;
        }

        user.UpdatePatientProfile(request.BirthDate, request.BiologicalSex, request.HeightInCm, request.Goal);
        user.UpdatePatientRoutine(request.WakeUpTime, request.BedTime, request.TrainingTime);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var photoUrl = await fileStorage.PatientPhotoUrlAsync(user.PatientProfile.PhotoPath, cancellationToken);
        return PatientProfileResponse.From(user, user.PatientProfile, today, photoUrl);
    }
}
