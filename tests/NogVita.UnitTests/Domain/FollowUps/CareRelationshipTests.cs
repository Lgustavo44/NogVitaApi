using NogVita.Domain.Common;
using NogVita.Domain.FollowUps;

namespace NogVita.UnitTests.Domain.FollowUps;

public class CareRelationshipTests
{
    private static readonly Guid PatientId = Guid.NewGuid();
    private static readonly Guid NutritionistId = Guid.NewGuid();
    private static readonly DateTime Now = new(2026, 10, 9, 12, 0, 0, DateTimeKind.Utc);

    private static CareRelationship CreateActive() =>
        new NutritionistRequest(PatientId, NutritionistId, null).Accept(Now);

    [Fact]
    public void Patient_Can_End()
    {
        var relationship = CreateActive();

        relationship.End(PatientId, Now.AddDays(30));

        Assert.False(relationship.IsActive);
        Assert.Equal(Now.AddDays(30), relationship.EndedAtUtc);
        Assert.Equal(PatientId, relationship.EndedByUserId);
    }

    [Fact]
    public void Nutritionist_Can_End()
    {
        var relationship = CreateActive();

        relationship.End(NutritionistId, Now.AddDays(30));

        Assert.False(relationship.IsActive);
        Assert.Equal(NutritionistId, relationship.EndedByUserId);
    }

    [Fact]
    public void Stranger_Cannot_End()
    {
        var relationship = CreateActive();

        Assert.Throws<DomainException>(() => relationship.End(Guid.NewGuid(), Now));
        Assert.True(relationship.IsActive);
    }

    [Fact]
    public void Should_Not_End_Twice()
    {
        var relationship = CreateActive();
        relationship.End(PatientId, Now);

        Assert.Throws<DomainException>(() => relationship.End(NutritionistId, Now));
    }
}