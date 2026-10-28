using NogVita.Domain.FollowUps;
using NogVita.Domain.Users;
using NogVita.UnitTests.Fakes;

namespace NogVita.UnitTests;

// Os usuários prontos dos testes da Application (Fase 23): um nutricionista ativo, um paciente, um pré-cadastro
// e o vínculo entre eles. Cada teste diz só o que é diferente; a montagem fica num lugar só.
// Os testes do Domain continuam montando à mão, porque é a própria montagem que eles testam.
public static class TestUsers
{
    // Ativo, com o e-mail confirmado e o convite aceito: pode atender.
    public static User Nutritionist(
        string name = "Nutri Teste", string email = "nutri@email.com", string? cpf = null,
        int crnRegion = 1, string crnNumber = "123456")
    {
        var nutritionist = new User(name, email, Cpf.Create(cpf ?? TestCpf.Next()));
        nutritionist.SetPasswordHash("hash");
        nutritionist.ConfirmEmail(DateTime.UtcNow);
        nutritionist.CreateNutritionistProfile(crnRegion, crnNumber);
        nutritionist.NutritionistProfile!.Activate();
        return nutritionist;
    }

    // Pré-cadastrado pelo admin: sem senha, e com o convite ainda não aceito.
    public static User PendingNutritionist(
        string name = "Ana Lima", string email = "ana@nogvita.local", string cpf = "123.456.789-09",
        int crnRegion = 3, string crnNumber = "12345")
    {
        var nutritionist = new User(name, email, Cpf.Create(cpf));
        nutritionist.CreateNutritionistProfile(crnRegion, crnNumber);
        return nutritionist;
    }

    // Ativo, com o e-mail confirmado e o perfil de paciente.
    public static User Patient(
        string name = "Paciente Teste", string email = "paciente@email.com", string? cpf = null,
        DateOnly? birthDate = null, BiologicalSex biologicalSex = BiologicalSex.Female, int heightInCm = 165,
        Goal goal = Goal.WeightLoss)
    {
        var patient = new User(name, email, Cpf.Create(cpf ?? TestCpf.Next()));
        patient.SetPasswordHash("hash");
        patient.ConfirmEmail(DateTime.UtcNow);
        patient.CreatePatientProfile(birthDate ?? new DateOnly(1995, 3, 10), biologicalSex, heightInCm, goal);
        return patient;
    }

    // O vínculo ativo, como ele nasce de verdade: uma solicitação aceita.
    public static CareRelationship Follow(Guid patientId, Guid nutritionistId) =>
        new NutritionistRequest(patientId, nutritionistId, null).Accept(DateTime.UtcNow);

    public static User AddTo(this User user, FakeUserRepository repository)
    {
        repository.Add(user);
        return user;
    }
}
