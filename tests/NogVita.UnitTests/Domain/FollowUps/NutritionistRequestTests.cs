using NogVita.Domain.Common;
using NogVita.Domain.FollowUps;

namespace NogVita.UnitTests.Domain.FollowUps;

public class NutritionistRequestTests
{
    private static readonly Guid PatientId = Guid.NewGuid();
    private static readonly Guid NutritionistId = Guid.NewGuid();
    private static readonly DateTime Now = new(2026, 10, 9, 12, 0, 0, DateTimeKind.Utc);

    private static NutritionistRequest CreatePending(string? message = null) =>
        new(PatientId, NutritionistId, message);

    [Fact]
    public void Should_Start_As_Pending()
    {
        var request = CreatePending("  Quero ganhar massa.  ");

        Assert.Equal(NutritionistRequestStatus.Pending, request.Status);
        Assert.Equal("Quero ganhar massa.", request.Message);
        Assert.Null(request.RespondedAtUtc);
    }

    [Fact]
    public void Should_Not_Allow_Request_To_Self()
    {
        Assert.Throws<DomainException>(() => new NutritionistRequest(PatientId, PatientId, null));
    }

    [Fact]
    public void Should_Not_Accept_Message_Longer_Than_Limit()
    {
        var message = new string('a', NutritionistRequest.MessageMaxLength + 1);

        Assert.Throws<DomainException>(() => CreatePending(message));
    }

    [Fact]
    public void Should_Create_Care_Relationship_When_Accepted()
    {
        var request = CreatePending();

        var relationship = request.Accept(Now);

        Assert.Equal(NutritionistRequestStatus.Accepted, request.Status);
        Assert.Equal(Now, request.RespondedAtUtc);
        Assert.Equal(PatientId, relationship.PatientId);
        Assert.Equal(NutritionistId, relationship.NutritionistId);
        Assert.Equal(Now, relationship.StartedAtUtc);
        Assert.True(relationship.IsActive);
    }

    [Fact]
    public void Should_Reject()
    {
        var request = CreatePending();

        request.Reject(Now);

        Assert.Equal(NutritionistRequestStatus.Rejected, request.Status);
        Assert.Equal(Now, request.RespondedAtUtc);
    }

    [Fact]
    public void Should_Cancel()
    {
        var request = CreatePending();

        request.Cancel();

        Assert.Equal(NutritionistRequestStatus.Cancelled, request.Status);
        Assert.Null(request.RespondedAtUtc);
    }

    [Fact]
    public void Should_Not_Accept_After_Reject()
    {
        var request = CreatePending();
        request.Reject(Now);

        Assert.Throws<DomainException>(() => request.Accept(Now));
    }

    [Fact]
    public void Should_Not_Cancel_After_Accept()
    {
        var request = CreatePending();
        request.Accept(Now);

        Assert.Throws<DomainException>(() => request.Cancel());
    }

    [Fact]
    public void Should_Not_Accept_Twice()
    {
        var request = CreatePending();
        request.Accept(Now);

        Assert.Throws<DomainException>(() => request.Accept(Now));
    }
}