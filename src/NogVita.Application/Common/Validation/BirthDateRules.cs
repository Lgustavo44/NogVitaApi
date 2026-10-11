using FluentValidation;
using NogVita.Domain.Users;

namespace NogVita.Application.Common.Validation;

public static class BirthDateRules
{
    // Mesma regra do PatientProfile, aqui para responder 400 com o erro no campo (o domínio lançaria exceção).
    public static IRuleBuilderOptions<T, DateOnly> ValidPatientBirthDate<T>(this IRuleBuilder<T, DateOnly> rule, TimeProvider timeProvider)
    {
        return rule
            .Must(birthDate => birthDate <= Today(timeProvider))
            .WithMessage("A data de nascimento não pode estar no futuro.")
            .Must(birthDate => PatientProfile.CalculateAge(birthDate, Today(timeProvider)) >= PatientProfile.MinimumAge)
            .WithMessage($"É preciso ter pelo menos {PatientProfile.MinimumAge} anos para usar o NogVita.");
    }

    private static DateOnly Today(TimeProvider timeProvider) =>
        DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
}
