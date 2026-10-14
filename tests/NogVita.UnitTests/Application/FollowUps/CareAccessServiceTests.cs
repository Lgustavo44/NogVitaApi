using NogVita.Application.FollowUps;
using NogVita.Domain.FollowUps;
using NogVita.Domain.Users;
using NogVita.UnitTests.Fakes;

namespace NogVita.UnitTests.Application.FollowUps;

public class CareAccessServiceTests
{
    private readonly FakeUserRepository _userRepository = new();
    private readonly FakeCareRelationshipRepository _relationshipRepository = new();
    private readonly Guid _patientId = Guid.NewGuid();
    private readonly User _nutritionist;

    public CareAccessServiceTests()
    {
        _nutritionist = new User("Nutri Teste", "nutri@email.com", Cpf.Create("12345678909"));
        _nutritionist.SetPasswordHash("hash");
        _nutritionist.ConfirmEmail(DateTime.UtcNow);
        _nutritionist.CreateNutritionistProfile(1, "123456");
        _nutritionist.NutritionistProfile!.Activate();
        _userRepository.Add(_nutritionist);
    }

    private CareAccessService CreateService() => new(_userRepository, _relationshipRepository);

    private CareRelationship Link(Guid nutritionistId)
    {
        var relationship = new NutritionistRequest(_patientId, nutritionistId, null).Accept(DateTime.UtcNow);
        _relationshipRepository.Add(relationship);
        return relationship;
    }

    [Fact]
    public async Task Should_Allow_Nutritionist_With_Active_Relationship()
    {
        Link(_nutritionist.Id);

        Assert.True(await CreateService().NutritionistFollowsPatientAsync(
            _nutritionist.Id, _patientId, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Should_Deny_When_Patient_Follows_Another_Nutritionist()
    {
        Link(Guid.NewGuid());

        Assert.False(await CreateService().NutritionistFollowsPatientAsync(
            _nutritionist.Id, _patientId, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Should_Deny_After_Relationship_Ended()
    {
        var relationship = Link(_nutritionist.Id);
        relationship.End(_patientId, DateTime.UtcNow);

        Assert.False(await CreateService().NutritionistFollowsPatientAsync(
            _nutritionist.Id, _patientId, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Should_Deny_Deactivated_Nutritionist()
    {
        Link(_nutritionist.Id);
        _nutritionist.Deactivate();

        Assert.False(await CreateService().NutritionistFollowsPatientAsync(
            _nutritionist.Id, _patientId, TestContext.Current.CancellationToken));
    }
}