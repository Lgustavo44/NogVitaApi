using NogVita.Application.Abstractions;
using NogVita.Application.Photos;

namespace NogVita.Application.Patients;

public sealed class GetMyPatientProfileUseCase(IUserRepository userRepository, IFileStorage fileStorage, TimeProvider timeProvider)
{
    public async Task<PatientProfileResponse?> ExecuteAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);

        if (user is null || !user.IsActive || user.PatientProfile is null)
        {
            return null;
        }

        if (user?.PatientProfile is null)
        {
            return null;
        }

        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

        var photoUrl = await fileStorage.PatientPhotoUrlAsync(user.PatientProfile.PhotoPath, cancellationToken);

        return PatientProfileResponse.From(user, user.PatientProfile, today, photoUrl);
    }
}
