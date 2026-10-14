using NogVita.Application.Abstractions;
using NogVita.Application.Common;
using NogVita.Domain.Users;

namespace NogVita.Application.FollowUps;

// Só o necessário para montar o plano (LGPD, art. 6º, III): a idade, e não a data de nascimento; sem CPF e sem e-mail.
public sealed record FollowedPatientResponse(
    Guid PatientId,
    string Name,
    int Age,
    BiologicalSex BiologicalSex,
    int HeightInCm,
    Goal Goal,
    TimeOnly? WakeUpTime,
    TimeOnly? BedTime,
    TimeOnly? TrainingTime);

public sealed class GetFollowedPatientUseCase(
    CareAccessService careAccessService,
    IUserRepository userRepository,
    TimeProvider timeProvider)
{
    public async Task<FollowedPatientResponse?> ExecuteAsync(
        Guid nutritionistId,
        Guid patientId,
        CancellationToken cancellationToken = default)
    {
        if (!await careAccessService.NutritionistFollowsPatientAsync(nutritionistId, patientId, cancellationToken))
        {
            return null;
        }

        var patient = await userRepository.GetByIdAsync(patientId, cancellationToken);

        if (patient?.PatientProfile is not { } profile)
        {
            return null;
        }

        return new FollowedPatientResponse(
            patient.Id,
            patient.Name,
            profile.GetAgeAt(BrazilianCalendar.Today(timeProvider)),
            profile.BiologicalSex,
            profile.HeightInCm,
            profile.Goal,
            profile.WakeUpTime,
            profile.BedTime,
            profile.TrainingTime);
    }
}
