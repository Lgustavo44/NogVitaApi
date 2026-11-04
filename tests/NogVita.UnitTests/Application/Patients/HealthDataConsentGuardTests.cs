using NogVita.Application.Patients;
using NogVita.Domain.Common;

namespace NogVita.UnitTests.Application.Patients;

public class HealthDataConsentGuardTests
{
    [Fact]
    public void Patient_with_current_consent_passes()
    {
        var patient = TestUsers.Patient();

        HealthDataConsentGuard.EnsureAccepted(patient);
    }

    [Fact]
    public void Patient_without_consent_is_refused()
    {
        var patient = TestUsers.Patient(healthDataConsent: false);

        var exception = Assert.Throws<DomainException>(() => HealthDataConsentGuard.EnsureAccepted(patient));
        Assert.Equal(HealthDataConsentMessages.ConsentRequired, exception.Message);
    }

    [Fact]
    public void Consent_of_an_old_version_is_refused()
    {
        var patient = TestUsers.Patient(healthDataConsent: false);
        patient.GrantHealthDataConsent("0", DateTime.UtcNow);

        Assert.Throws<DomainException>(() => HealthDataConsentGuard.EnsureAccepted(patient));
    }
}
