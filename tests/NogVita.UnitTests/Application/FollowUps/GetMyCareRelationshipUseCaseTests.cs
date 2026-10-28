using NogVita.Application.FollowUps;
using NogVita.Domain.Users;
using NogVita.UnitTests.Fakes;

namespace NogVita.UnitTests.Application.FollowUps;

public class GetMyCareRelationshipUseCaseTests
{
    private readonly FakeUserRepository _userRepository = new();
    private readonly FakeCareRelationshipRepository _relationshipRepository = new();
    private readonly User _patient;
    private readonly User _nutritionist;

    public GetMyCareRelationshipUseCaseTests()
    {
        _patient = TestUsers.Patient().AddTo(_userRepository);
        _nutritionist = TestUsers.Nutritionist(name: "Ana Souza", crnRegion: 3, crnNumber: "12345").AddTo(_userRepository);
        _nutritionist.UpdateNutritionistProfile("Nutrição esportiva.");
    }

    private GetMyCareRelationshipUseCase CreateUseCase() => new(_relationshipRepository, _userRepository);

    [Fact]
    public async Task Should_Return_Active_Relationship_With_Nutritionist_Data()
    {
        var relationship = TestUsers.Follow(_patient.Id, _nutritionist.Id);
        _relationshipRepository.Add(relationship);

        var response = await CreateUseCase().ExecuteAsync(_patient.Id, TestContext.Current.CancellationToken);

        Assert.NotNull(response);
        Assert.Equal(relationship.Id, response.RelationshipId);
        Assert.Equal(_nutritionist.Id, response.NutritionistId);
        Assert.Equal("Ana Souza", response.NutritionistName);
        Assert.Equal(3, response.CrnRegion);
        Assert.Equal("12345", response.CrnNumber);
        Assert.Equal("Nutrição esportiva.", response.Bio);
        Assert.Equal(relationship.StartedAtUtc, response.StartedAtUtc);
    }

    [Fact]
    public async Task Should_Return_Null_Without_Relationship()
    {
        Assert.Null(await CreateUseCase().ExecuteAsync(_patient.Id, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Should_Return_Null_After_Relationship_Ended()
    {
        var relationship = TestUsers.Follow(_patient.Id, _nutritionist.Id);
        relationship.End(_patient.Id, DateTime.UtcNow);
        _relationshipRepository.Add(relationship);

        Assert.Null(await CreateUseCase().ExecuteAsync(_patient.Id, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Should_Return_Null_When_Nutritionist_No_Longer_Exists()
    {
        _relationshipRepository.Add(TestUsers.Follow(_patient.Id, Guid.NewGuid()));

        Assert.Null(await CreateUseCase().ExecuteAsync(_patient.Id, TestContext.Current.CancellationToken));
    }
}
