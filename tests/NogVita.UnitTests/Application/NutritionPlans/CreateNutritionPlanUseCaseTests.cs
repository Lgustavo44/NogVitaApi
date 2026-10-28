using NogVita.Application.FollowUps;
using NogVita.Application.NutritionPlans;
using NogVita.Domain.FollowUps;
using NogVita.Domain.NutritionPlans;
using NogVita.Domain.Users;
using NogVita.UnitTests.Fakes;

namespace NogVita.UnitTests.Application.NutritionPlans;

public class CreateNutritionPlanUseCaseTests
{
    private readonly FakeUserRepository _userRepository = new();
    private readonly FakeCareRelationshipRepository _relationshipRepository = new();
    private readonly FakeNutritionPlanRepository _planRepository = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly Guid _patientId;
    private readonly User _nutritionist;

    public CreateNutritionPlanUseCaseTests()
    {
        _nutritionist = new User("Nutri Teste", "nutri@email.com", Cpf.Create("12345678909"));
        _nutritionist.SetPasswordHash("hash");
        _nutritionist.ConfirmEmail(DateTime.UtcNow);
        _nutritionist.CreateNutritionistProfile(1, "123456");
        _nutritionist.NutritionistProfile!.Activate();
        _userRepository.Add(_nutritionist);
        _patientId = TestData.AddPatient(_userRepository).Id;
    }

    private CreateNutritionPlanUseCase CreateUseCase() =>
        new(new CareAccessService(_userRepository, _relationshipRepository), _planRepository, _unitOfWork);

    private static CreateNutritionPlanRequest CreateRequest(Guid patientId) =>
        new(patientId, "Hipertrofia", "Ganho de massa.", new DateOnly(2026, 10, 15), null);

    [Fact]
    public async Task Should_Create_Draft_For_Linked_Patient()
    {
        _relationshipRepository.Add(new NutritionistRequest(_patientId, _nutritionist.Id, null).Accept(DateTime.UtcNow));

        var planId = await CreateUseCase().ExecuteAsync(_nutritionist.Id, CreateRequest(_patientId), TestContext.Current.CancellationToken);

        var plan = Assert.Single(_planRepository.Plans);
        Assert.Equal(plan.Id, planId);
        Assert.Equal(NutritionPlanStatus.Draft, plan.Status);
        Assert.Equal(_patientId, plan.PatientId);
        Assert.Equal(_nutritionist.Id, plan.NutritionistId);
        Assert.Equal(1, _unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task Should_Not_Create_For_Patient_Not_Linked()
    {
        var planId = await CreateUseCase().ExecuteAsync(_nutritionist.Id, CreateRequest(_patientId), TestContext.Current.CancellationToken);

        Assert.Null(planId);
        Assert.Empty(_planRepository.Plans);
        Assert.Equal(0, _unitOfWork.SaveChangesCount);
    }
}