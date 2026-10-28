using NogVita.Application.FollowUps;
using NogVita.Domain.FollowUps;
using NogVita.Domain.Users;
using NogVita.UnitTests.Fakes;

namespace NogVita.UnitTests;

// O cenário que quase todo teste do acompanhamento precisa (D176): um nutricionista ativo, um paciente,
// os dois repositórios falsos, e o vínculo entre eles. Cada teste muda só o que interessa a ele
// (encerrar o vínculo, desativar alguém, trocar o nutricionista).
public sealed class CareScenario
{
    public FakeUserRepository Users { get; } = new();
    public FakeCareRelationshipRepository Relationships { get; } = new();

    public User Nutritionist { get; }
    public User Patient { get; }

    // O vínculo ativo, ou null quando o cenário começa sem acompanhamento.
    public CareRelationship? Relationship { get; private set; }

    public CareScenario(bool following = true)
    {
        Nutritionist = TestUsers.Nutritionist().AddTo(Users);
        Patient = TestUsers.Patient().AddTo(Users);

        if (following)
        {
            Follow();
        }
    }

    public CareRelationship Follow()
    {
        Relationship = TestUsers.Follow(Patient.Id, Nutritionist.Id);
        Relationships.Add(Relationship);
        return Relationship;
    }

    // Outro nutricionista, ativo, que não acompanha o paciente.
    public User AnotherNutritionist() =>
        TestUsers.Nutritionist(name: "Outra Nutri", email: "outra@email.com", crnNumber: "999999").AddTo(Users);

    public CareAccessService CareAccess() => new(Users, Relationships);
}
