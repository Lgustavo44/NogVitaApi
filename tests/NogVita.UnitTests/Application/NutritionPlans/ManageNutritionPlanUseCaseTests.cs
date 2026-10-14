using NogVita.Application.FollowUps;
using NogVita.Application.NutritionPlans;
using NogVita.Domain.FollowUps;
using NogVita.Domain.NutritionPlans;
using NogVita.Domain.Users;
using NogVita.UnitTests.Fakes;

namespace NogVita.UnitTests.Application.NutritionPlans;

public class ManageNutritionPlanUseCaseTests
{
    private readonly FakeUserRepository _userRepository = new();
    private readonly FakeCareRelationshipRepository _relationshipRepository = new();
    private readonly FakeNutritionPlanRepository _planRepository = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly Guid _patientId = Guid.NewGuid();
    private readonly User _nutritionist;
    private readonly CareRelationship _relationship;

    public ManageNutritionPlanUseCaseTests()
    {
        _nutritionist = new User("Nutri Teste", "nutri@email.com", Cpf.Create("12345678909"));
        _nutritionist.SetPasswordHash("hash");
        _nutritionist.ConfirmEmail(DateTime.UtcNow);
        _nutritionist.CreateNutritionistProfile(1, "123456");
        _nutritionist.NutritionistProfile!.Activate();
        _userRepository.Add(_nutritionist);

        _relationship = new NutritionistRequest(_patientId, _nutritionist.Id, null).Accept(DateTime.UtcNow);
        _relationshipRepository.Add(_relationship);
    }

    private ManageNutritionPlanUseCase CreateUseCase() =>
        new(
            new NutritionistPlanAccess(new CareAccessService(_userRepository, _relationshipRepository), _planRepository),
            _planRepository,
            _unitOfWork,
            TimeProvider.System);

    private NutritionPlan AddPlan(Guid? nutritionistId = null, bool active = false)
    {
        var plan = new NutritionPlan(_patientId, nutritionistId ?? _nutritionist.Id, "Plano", null, new DateOnly(2026, 10, 1), null);
        if (active)
        {
            plan.Activate();
        }
        _planRepository.Add(plan);
        return plan;
    }

    [Fact]
    public async Task Activate_Should_Finish_Previous_Active_Plan_In_One_Transaction()
    {
        var oldPlan = AddPlan(active: true);
        var newPlan = AddPlan();

        var result = await CreateUseCase().ActivateAsync(_nutritionist.Id, newPlan.Id, TestContext.Current.CancellationToken);

        Assert.Equal(ManageNutritionPlanResult.Done, result);
        Assert.Equal(NutritionPlanStatus.Finished, oldPlan.Status);
        Assert.Equal(NutritionPlanStatus.Active, newPlan.Status);
        Assert.Equal(1, _unitOfWork.TransactionCount);
        Assert.Equal(2, _unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task Activate_Without_Previous_Plan_Should_Save_Once()
    {
        var plan = AddPlan();

        await CreateUseCase().ActivateAsync(_nutritionist.Id, plan.Id, TestContext.Current.CancellationToken);

        Assert.Equal(NutritionPlanStatus.Active, plan.Status);
        Assert.Equal(1, _unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task Should_Not_Touch_Plan_Of_Another_Nutritionist()
    {
        var plan = AddPlan(nutritionistId: Guid.NewGuid());

        var result = await CreateUseCase().ActivateAsync(_nutritionist.Id, plan.Id, TestContext.Current.CancellationToken);

        Assert.Equal(ManageNutritionPlanResult.NotFound, result);
        Assert.Equal(NutritionPlanStatus.Draft, plan.Status);
    }

    [Fact]
    public async Task Should_Lose_Access_After_Relationship_Ended()
    {
        var plan = AddPlan();
        _relationship.End(_patientId, DateTime.UtcNow);

        var result = await CreateUseCase().ActivateAsync(_nutritionist.Id, plan.Id, TestContext.Current.CancellationToken);

        Assert.Equal(ManageNutritionPlanResult.NotFound, result);
    }

    [Fact]
    public async Task Should_Not_Update_Finished_Plan()
    {
        var plan = AddPlan(active: true);
        plan.Finish(new DateOnly(2026, 10, 20));

        var result = await CreateUseCase().UpdateAsync(
            _nutritionist.Id,
            plan.Id,
            new UpdateNutritionPlanRequest("Novo nome", null, new DateOnly(2026, 10, 1), null),
            TestContext.Current.CancellationToken);

        Assert.Equal(ManageNutritionPlanResult.InvalidState, result);
        Assert.Equal("Plano", plan.Name);
        Assert.Equal(0, _unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task Should_Not_Cancel_Active_Plan()
    {
        var plan = AddPlan(active: true);

        var result = await CreateUseCase().CancelAsync(_nutritionist.Id, plan.Id, TestContext.Current.CancellationToken);

        Assert.Equal(ManageNutritionPlanResult.InvalidState, result);
        Assert.Equal(NutritionPlanStatus.Active, plan.Status);
    }

    [Fact]
    public async Task Should_Finish_Active_Plan()
    {
        var plan = AddPlan(active: true);

        var result = await CreateUseCase().FinishAsync(_nutritionist.Id, plan.Id, TestContext.Current.CancellationToken);

        Assert.Equal(ManageNutritionPlanResult.Done, result);
        Assert.Equal(NutritionPlanStatus.Finished, plan.Status);
        Assert.NotNull(plan.EndDate);
        Assert.Equal(1, _unitOfWork.SaveChangesCount);
    }
}