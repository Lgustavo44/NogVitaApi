using NogVita.Application.FollowUps;
using NogVita.Domain.FollowUps;
using NogVita.Domain.Users;
using NogVita.UnitTests.Fakes;

namespace NogVita.UnitTests.Application.FollowUps;

public class RespondNutritionistRequestUseCaseTests
{
    private readonly FakeUserRepository _userRepository = new();
    private readonly FakeNutritionistRequestRepository _requestRepository = new();
    private readonly FakeCareRelationshipRepository _relationshipRepository = new();
    private readonly FakeUnitOfWork _unitOfWork = new();

    private readonly Guid _patientId = Guid.NewGuid();
    private readonly User _nutritionist;

    public RespondNutritionistRequestUseCaseTests()
    {
        _nutritionist = new User("Nutri Teste", "nutri@email.com", Cpf.Create("12345678909"));
        _nutritionist.SetPasswordHash("hash");
        _nutritionist.ConfirmEmail(DateTime.UtcNow);
        _nutritionist.CreateNutritionistProfile(1, "123456");
        _nutritionist.NutritionistProfile!.Activate();
        _userRepository.Add(_nutritionist);
    }

    private RespondNutritionistRequestUseCase CreateUseCase() =>
        new(_requestRepository, _relationshipRepository, _userRepository, _unitOfWork, TimeProvider.System);

    private NutritionistRequest AddPending(Guid nutritionistId)
    {
        var request = new NutritionistRequest(_patientId, nutritionistId, null);
        _requestRepository.Add(request);
        return request;
    }

    [Fact]
    public async Task Should_Accept_And_Create_Relationship()
    {
        var request = AddPending(_nutritionist.Id);

        var result = await CreateUseCase().AcceptAsync(_nutritionist.Id, request.Id, TestContext.Current.CancellationToken);

        Assert.Equal(RespondNutritionistRequestResult.Done, result);
        Assert.Equal(NutritionistRequestStatus.Accepted, request.Status);
        var relationship = Assert.Single(_relationshipRepository.Relationships);
        Assert.Equal(_patientId, relationship.PatientId);
        Assert.Equal(_nutritionist.Id, relationship.NutritionistId);
        Assert.Equal(1, _unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task Should_Reject_Without_Relationship()
    {
        var request = AddPending(_nutritionist.Id);

        var result = await CreateUseCase().RejectAsync(_nutritionist.Id, request.Id, TestContext.Current.CancellationToken);

        Assert.Equal(RespondNutritionistRequestResult.Done, result);
        Assert.Equal(NutritionistRequestStatus.Rejected, request.Status);
        Assert.Empty(_relationshipRepository.Relationships);
    }

    [Fact]
    public async Task Should_Not_Accept_Request_Of_Another_Nutritionist()
    {
        var request = AddPending(Guid.NewGuid());

        var result = await CreateUseCase().AcceptAsync(_nutritionist.Id, request.Id, TestContext.Current.CancellationToken);

        Assert.Equal(RespondNutritionistRequestResult.NotFound, result);
        Assert.True(request.IsPending);
        Assert.Empty(_relationshipRepository.Relationships);
        Assert.Equal(0, _unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task Should_Not_Accept_Twice()
    {
        var request = AddPending(_nutritionist.Id);
        var useCase = CreateUseCase();
        await useCase.AcceptAsync(_nutritionist.Id, request.Id, TestContext.Current.CancellationToken);

        var secondAttempt = await useCase.AcceptAsync(_nutritionist.Id, request.Id, TestContext.Current.CancellationToken);

        Assert.Equal(RespondNutritionistRequestResult.NotPending, secondAttempt);
        Assert.Single(_relationshipRepository.Relationships);
    }
}