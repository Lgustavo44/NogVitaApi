using NogVita.Application.FollowUps;
using NogVita.Application.NutritionPlans;
using NogVita.Domain.FollowUps;
using NogVita.Domain.NutritionPlans;
using NogVita.Domain.Users;
using NogVita.UnitTests.Fakes;

namespace NogVita.UnitTests.Application.NutritionPlans;

public class CreateNutritionPlanUseCaseTests
{
    private readonly CareScenario _care = new(following: false);
    private readonly FakeNutritionPlanRepository _planRepository = new();
    private readonly FakeUnitOfWork _unitOfWork = new();

    private CreateNutritionPlanUseCase CreateUseCase() =>
        new(_care.CareAccess(), _planRepository, _unitOfWork);

    private static CreateNutritionPlanRequest CreateRequest(Guid patientId) =>
        new(patientId, "Hipertrofia", "Ganho de massa.", new DateOnly(2026, 10, 15), null);

    [Fact]
    public async Task Should_Create_Draft_For_Linked_Patient()
    {
        _care.Follow();

        var planId = await CreateUseCase().ExecuteAsync(_care.Nutritionist.Id, CreateRequest(_care.Patient.Id), TestContext.Current.CancellationToken);

        var plan = Assert.Single(_planRepository.Plans);
        Assert.Equal(plan.Id, planId);
        Assert.Equal(NutritionPlanStatus.Draft, plan.Status);
        Assert.Equal(_care.Patient.Id, plan.PatientId);
        Assert.Equal(_care.Nutritionist.Id, plan.NutritionistId);
        Assert.Equal(1, _unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task Should_Not_Create_For_Patient_Not_Linked()
    {
        var planId = await CreateUseCase().ExecuteAsync(_care.Nutritionist.Id, CreateRequest(_care.Patient.Id), TestContext.Current.CancellationToken);

        Assert.Null(planId);
        Assert.Empty(_planRepository.Plans);
        Assert.Equal(0, _unitOfWork.SaveChangesCount);
    }
}
