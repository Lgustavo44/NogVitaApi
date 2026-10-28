using NogVita.Domain.Users;
using NogVita.UnitTests.Fakes;

namespace NogVita.UnitTests;

public static class TestData
{
    public static readonly Cpf ValidCpf = Cpf.Create("12345678909");

    public static User CreateUser() => new("Maria Silva", "maria@email.com", ValidCpf);

    // O CareAccessService confere o paciente no repositório (D163): os testes com acompanhamento precisam dele.
    public static User AddPatient(FakeUserRepository userRepository)
    {
        var patient = new User("Paciente Teste", "paciente@email.com", Cpf.Create("11144477735"));
        patient.SetPasswordHash("hash");
        patient.ConfirmEmail(DateTime.UtcNow);
        patient.CreatePatientProfile(new DateOnly(1995, 3, 10), BiologicalSex.Female, 165, Goal.WeightLoss);
        userRepository.Add(patient);
        return patient;
    }
}
