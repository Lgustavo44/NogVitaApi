using NogVita.Domain.Common;
using NogVita.Domain.Users;

namespace NogVita.Application.Patients;

// D-V2-22: sem o aceite da versão atual, nenhum dado de saúde novo é criado (LGPD, art. 11, I).
// O front também cobra o aceite, mas o front sozinho não basta: a API é chamada direto, sem passar por ele.
// As leituras continuam livres: acessar os próprios dados é direito do paciente (art. 18, II).
public static class HealthDataConsentGuard
{
    public static void EnsureAccepted(User patient)
    {
        if (patient.PatientProfile?.HasHealthDataConsent(HealthDataConsent.CurrentVersion) != true)
        {
            throw new DomainException(HealthDataConsentMessages.ConsentRequired);
        }
    }
}
