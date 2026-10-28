using NogVita.Application.FollowUps;
using NogVita.Domain.FollowUps;
using NogVita.Domain.Users;
using NogVita.UnitTests.Fakes;

namespace NogVita.UnitTests.Application.FollowUps;

public class CareAccessServiceTests
{
    private readonly CareScenario _care = new(following: false);

    private CareAccessService CreateService() => _care.CareAccess();

    private CareRelationship Link(Guid nutritionistId)
    {
        var relationship = TestUsers.Follow(_care.Patient.Id, nutritionistId);
        _care.Relationships.Add(relationship);
        return relationship;
    }

    [Fact]
    public async Task Should_Allow_Nutritionist_With_Active_Relationship()
    {
        Link(_care.Nutritionist.Id);

        Assert.True(await CreateService().NutritionistFollowsPatientAsync(
            _care.Nutritionist.Id, _care.Patient.Id, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Should_Deny_When_Patient_Follows_Another_Nutritionist()
    {
        Link(Guid.NewGuid());

        Assert.False(await CreateService().NutritionistFollowsPatientAsync(
            _care.Nutritionist.Id, _care.Patient.Id, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Should_Deny_After_Relationship_Ended()
    {
        var relationship = Link(_care.Nutritionist.Id);
        relationship.End(_care.Patient.Id, DateTime.UtcNow);

        Assert.False(await CreateService().NutritionistFollowsPatientAsync(
            _care.Nutritionist.Id, _care.Patient.Id, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Should_Deny_Deactivated_Nutritionist()
    {
        Link(_care.Nutritionist.Id);
        _care.Nutritionist.Deactivate(DateTime.UtcNow);

        Assert.False(await CreateService().NutritionistFollowsPatientAsync(
            _care.Nutritionist.Id, _care.Patient.Id, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Should_Deny_Patient_Deactivated_By_Admin()
    {
        Link(_care.Nutritionist.Id);
        _care.Patient.Deactivate(DateTime.UtcNow);

        Assert.False(await CreateService().NutritionistFollowsPatientAsync(
            _care.Nutritionist.Id, _care.Patient.Id, TestContext.Current.CancellationToken));
    }
}
