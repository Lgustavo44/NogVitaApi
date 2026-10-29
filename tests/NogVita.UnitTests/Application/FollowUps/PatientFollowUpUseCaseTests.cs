using Microsoft.Extensions.Logging.Abstractions;
using NogVita.Application.FollowUps;
using NogVita.Domain.FollowUps;
using NogVita.Domain.NutritionPlans;
using NogVita.Domain.Users;
using NogVita.UnitTests.Fakes;

namespace NogVita.UnitTests.Application.FollowUps;

public class PatientFollowUpUseCaseTests
{
    private readonly FakeUserRepository _userRepository = new();
    private readonly FakeNutritionistRequestRepository _requestRepository = new();
    private readonly FakeCareRelationshipRepository _relationshipRepository = new();
    private readonly FakeNutritionPlanRepository _planRepository = new();
    private readonly FakeEmailSender _emailSender = new();
    private readonly FakeUnitOfWork _unitOfWork = new();

    private readonly User _patient = new("Paciente Teste", "paciente@email.com", Cpf.Create("11144477735"));
    private readonly User _nutritionist = new("Nutri Teste", "nutri@email.com", Cpf.Create("12345678909"));

    public PatientFollowUpUseCaseTests()
    {
        _userRepository.Add(_patient);
        _userRepository.Add(_nutritionist);
    }

    private EndCareRelationshipUseCase CreateEndUseCase() =>
        new(
            _relationshipRepository,
            _userRepository,
            _planRepository,
            new FollowUpNotificationService(_emailSender, NullLogger<FollowUpNotificationService>.Instance),
            _unitOfWork,
            TimeProvider.System);

    private CareRelationship AddActiveRelationship()
    {
        var relationship = TestUsers.Follow(_patient.Id, _nutritionist.Id);
        _relationshipRepository.Add(relationship);
        return relationship;
    }

    [Fact]
    public async Task Patient_Should_Cancel_Own_Pending_Request()
    {
        var request = new NutritionistRequest(_patient.Id, _nutritionist.Id, null);
        _requestRepository.Add(request);

        var result = await new CancelNutritionistRequestUseCase(_requestRepository, _unitOfWork)
            .ExecuteAsync(_patient.Id, request.Id, TestContext.Current.CancellationToken);

        Assert.Equal(CancelNutritionistRequestResult.Cancelled, result);
        Assert.Equal(NutritionistRequestStatus.Cancelled, request.Status);
        Assert.Equal(1, _unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task Patient_Should_Not_Cancel_Request_Already_Answered()
    {
        var request = new NutritionistRequest(_patient.Id, _nutritionist.Id, null);
        request.Reject(DateTime.UtcNow);
        _requestRepository.Add(request);

        var result = await new CancelNutritionistRequestUseCase(_requestRepository, _unitOfWork)
            .ExecuteAsync(_patient.Id, request.Id, TestContext.Current.CancellationToken);

        Assert.Equal(CancelNutritionistRequestResult.NotPending, result);
        Assert.Equal(NutritionistRequestStatus.Rejected, request.Status);
        Assert.Equal(0, _unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task Patient_Should_Not_Cancel_Request_Of_Another_Patient()
    {
        var request = new NutritionistRequest(_patient.Id, _nutritionist.Id, null);
        _requestRepository.Add(request);

        var result = await new CancelNutritionistRequestUseCase(_requestRepository, _unitOfWork)
            .ExecuteAsync(Guid.NewGuid(), request.Id, TestContext.Current.CancellationToken);

        Assert.Equal(CancelNutritionistRequestResult.NotFound, result);
        Assert.True(request.IsPending);
    }

    [Fact]
    public async Task Patient_Should_End_And_Notify_Nutritionist()
    {
        var relationship = AddActiveRelationship();

        var ended = await CreateEndUseCase().ExecuteAsync(_patient.Id, _patient.Id, TestContext.Current.CancellationToken);

        Assert.True(ended);
        Assert.False(relationship.IsActive);
        Assert.Equal(_patient.Id, relationship.EndedByUserId);
        Assert.Equal(1, _unitOfWork.SaveChangesCount);

        var email = Assert.Single(_emailSender.SentMessages);
        Assert.Equal("nutri@email.com", email.To);
    }

    [Fact]
    public async Task Ending_Should_Finish_Active_Plan_And_Cancel_Drafts()
    {
        AddActiveRelationship();
        var activePlan = new NutritionPlan(_patient.Id, _nutritionist.Id, "Ativo", null, new DateOnly(2026, 10, 1), null);
        activePlan.Activate();
        var draft = new NutritionPlan(_patient.Id, _nutritionist.Id, "Rascunho", null, new DateOnly(2026, 11, 1), null);
        var otherNutritionistPlan = new NutritionPlan(_patient.Id, Guid.NewGuid(), "De outra nutri", null, new DateOnly(2026, 1, 1), null);
        _planRepository.Add(activePlan);
        _planRepository.Add(draft);
        _planRepository.Add(otherNutritionistPlan);

        await CreateEndUseCase().ExecuteAsync(_patient.Id, _patient.Id, TestContext.Current.CancellationToken);

        Assert.Equal(NutritionPlanStatus.Finished, activePlan.Status);
        Assert.Equal(NutritionPlanStatus.Cancelled, draft.Status);
        Assert.Equal(NutritionPlanStatus.Draft, otherNutritionistPlan.Status);
        Assert.Equal(1, _unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task Stranger_Should_Not_End_Relationship()
    {
        var relationship = AddActiveRelationship();

        var ended = await CreateEndUseCase().ExecuteAsync(Guid.NewGuid(), _patient.Id, TestContext.Current.CancellationToken);

        Assert.False(ended);
        Assert.True(relationship.IsActive);
        Assert.Empty(_emailSender.SentMessages);
    }

    [Fact]
    public async Task Should_Keep_End_When_Email_Fails()
    {
        var relationship = AddActiveRelationship();
        _emailSender.ShouldFail = true;

        var ended = await CreateEndUseCase().ExecuteAsync(_patient.Id, _patient.Id, TestContext.Current.CancellationToken);

        Assert.True(ended);
        Assert.False(relationship.IsActive);
        Assert.Equal(1, _unitOfWork.SaveChangesCount);
    }
}
