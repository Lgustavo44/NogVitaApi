using NogVita.Application.FollowUps;
using NogVita.Domain.FollowUps;
using NogVita.Domain.Users;
using NogVita.UnitTests.Fakes;

namespace NogVita.UnitTests.Application.FollowUps;

public class RequestNutritionistUseCaseTests
{
    private readonly FakeUserRepository _userRepository = new();
    private readonly FakeNutritionistRequestRepository _requestRepository = new();
    private readonly FakeCareRelationshipRepository _relationshipRepository = new();
    private readonly FakeUnitOfWork _unitOfWork = new();

    private RequestNutritionistUseCase CreateUseCase() =>
        new(_userRepository, _requestRepository, _relationshipRepository, _unitOfWork);

    private User AddPatient()
    {
        var user = new User("Paciente Teste", "paciente@email.com", Cpf.Create("11144477735"));
        user.SetPasswordHash("hash");
        user.ConfirmEmail(DateTime.UtcNow);
        user.CreatePatientProfile(new DateOnly(1995, 3, 10), BiologicalSex.Male, 178, Goal.MuscleGain);
        _userRepository.Add(user);
        return user;
    }

    private User AddNutritionist(bool active = true)
    {
        var user = new User("Nutri Teste", "nutri@email.com", Cpf.Create("12345678909"));
        user.SetPasswordHash("hash");
        user.ConfirmEmail(DateTime.UtcNow);
        user.CreateNutritionistProfile(1, "123456");
        if (active)
        {
            user.NutritionistProfile!.Activate();
        }
        _userRepository.Add(user);
        return user;
    }

    [Fact]
    public async Task Should_Create_Pending_Request()
    {
        var patient = AddPatient();
        var nutritionist = AddNutritionist();

        var result = await CreateUseCase().ExecuteAsync(
            patient.Id,
            new RequestNutritionistRequest(nutritionist.Id, "Quero ganhar massa."),
            TestContext.Current.CancellationToken);

        Assert.Equal(RequestNutritionistStatus.Created, result.Status);
        var request = Assert.Single(_requestRepository.Requests);
        Assert.Equal(result.RequestId, request.Id);
        Assert.Equal(patient.Id, request.PatientId);
        Assert.Equal(nutritionist.Id, request.NutritionistId);
        Assert.Equal(1, _unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task Should_Not_Request_To_Self()
    {
        var patient = AddPatient();

        var result = await CreateUseCase().ExecuteAsync(
            patient.Id,
            new RequestNutritionistRequest(patient.Id, null),
            TestContext.Current.CancellationToken);

        Assert.Equal(RequestNutritionistStatus.CannotRequestSelf, result.Status);
        Assert.Empty(_requestRepository.Requests);
    }

    [Fact]
    public async Task Should_Not_Request_Inactive_Nutritionist()
    {
        var patient = AddPatient();
        var nutritionist = AddNutritionist(active: false);

        var result = await CreateUseCase().ExecuteAsync(
            patient.Id,
            new RequestNutritionistRequest(nutritionist.Id, null),
            TestContext.Current.CancellationToken);

        Assert.Equal(RequestNutritionistStatus.NutritionistNotFound, result.Status);
    }

    [Fact]
    public async Task Should_Not_Request_When_Target_Is_Not_Nutritionist()
    {
        var patient = AddPatient();

        var result = await CreateUseCase().ExecuteAsync(
            patient.Id,
            new RequestNutritionistRequest(Guid.NewGuid(), null),
            TestContext.Current.CancellationToken);

        Assert.Equal(RequestNutritionistStatus.NutritionistNotFound, result.Status);
    }

    [Fact]
    public async Task Should_Not_Allow_Second_Pending_Request()
    {
        var patient = AddPatient();
        var nutritionist = AddNutritionist();
        _requestRepository.Add(new NutritionistRequest(patient.Id, nutritionist.Id, null));

        var result = await CreateUseCase().ExecuteAsync(
            patient.Id,
            new RequestNutritionistRequest(nutritionist.Id, null),
            TestContext.Current.CancellationToken);

        Assert.Equal(RequestNutritionistStatus.PendingRequestExists, result.Status);
        Assert.Single(_requestRepository.Requests);
    }

    [Fact]
    public async Task Should_Not_Request_When_Already_Has_Nutritionist()
    {
        var patient = AddPatient();
        var nutritionist = AddNutritionist();
        _relationshipRepository.Add(new NutritionistRequest(patient.Id, nutritionist.Id, null).Accept(DateTime.UtcNow));

        var result = await CreateUseCase().ExecuteAsync(
            patient.Id,
            new RequestNutritionistRequest(nutritionist.Id, null),
            TestContext.Current.CancellationToken);

        Assert.Equal(RequestNutritionistStatus.AlreadyHasNutritionist, result.Status);
        Assert.Equal(0, _unitOfWork.SaveChangesCount);
    }
}