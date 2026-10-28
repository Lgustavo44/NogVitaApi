using NogVita.Domain.Common;
using NogVita.Application.FollowUps;
using NogVita.Application.NutritionPlans;
using NogVita.Domain.FollowUps;
using NogVita.Domain.NutritionPlans;
using NogVita.Domain.Users;
using NogVita.UnitTests.Fakes;

namespace NogVita.UnitTests.Application.NutritionPlans;

public class ManageNutritionPlanUseCaseTests
{
    private readonly CareScenario _care = new();
    private readonly FakeNutritionPlanRepository _planRepository = new();
    private readonly FakeUnitOfWork _unitOfWork = new();

    private ManageNutritionPlanUseCase CreateUseCase() =>
        new(
            new NutritionistPlanAccess(_care.CareAccess(), _planRepository),
            _planRepository,
            _unitOfWork,
            TimeProvider.System);

    private NutritionPlan AddPlan(Guid? nutritionistId = null, bool active = false)
    {
        var plan = new NutritionPlan(_care.Patient.Id, nutritionistId ?? _care.Nutritionist.Id, "Plano", null, new DateOnly(2026, 10, 1), null);
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

        var result = await CreateUseCase().ActivateAsync(_care.Nutritionist.Id, newPlan.Id, TestContext.Current.CancellationToken);

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

        await CreateUseCase().ActivateAsync(_care.Nutritionist.Id, plan.Id, TestContext.Current.CancellationToken);

        Assert.Equal(NutritionPlanStatus.Active, plan.Status);
        Assert.Equal(1, _unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task Should_Not_Touch_Plan_Of_Another_Nutritionist()
    {
        var plan = AddPlan(nutritionistId: Guid.NewGuid());

        var result = await CreateUseCase().ActivateAsync(_care.Nutritionist.Id, plan.Id, TestContext.Current.CancellationToken);

        Assert.Equal(ManageNutritionPlanResult.NotFound, result);
        Assert.Equal(NutritionPlanStatus.Draft, plan.Status);
    }

    [Fact]
    public async Task Should_Lose_Access_After_Relationship_Ended()
    {
        var plan = AddPlan();
        _care.Relationship!.End(_care.Patient.Id, DateTime.UtcNow);

        var result = await CreateUseCase().ActivateAsync(_care.Nutritionist.Id, plan.Id, TestContext.Current.CancellationToken);

        Assert.Equal(ManageNutritionPlanResult.NotFound, result);
    }

    [Fact]
    public async Task Should_Not_Update_Finished_Plan()
    {
        var plan = AddPlan(active: true);
        plan.Finish(new DateOnly(2026, 10, 20));

        var result = await CreateUseCase().UpdateAsync(
            _care.Nutritionist.Id,
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

        var result = await CreateUseCase().CancelAsync(_care.Nutritionist.Id, plan.Id, TestContext.Current.CancellationToken);

        Assert.Equal(ManageNutritionPlanResult.InvalidState, result);
        Assert.Equal(NutritionPlanStatus.Active, plan.Status);
    }

    [Fact]
    public async Task Should_Finish_Active_Plan()
    {
        var plan = AddPlan(active: true);

        var result = await CreateUseCase().FinishAsync(_care.Nutritionist.Id, plan.Id, TestContext.Current.CancellationToken);

        Assert.Equal(ManageNutritionPlanResult.Done, result);
        Assert.Equal(NutritionPlanStatus.Finished, plan.Status);
        Assert.NotNull(plan.EndDate);
        Assert.Equal(1, _unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task Should_Update_Draft_Details()
    {
        var plan = AddPlan();

        var result = await CreateUseCase().UpdateAsync(
            _care.Nutritionist.Id, plan.Id,
            new UpdateNutritionPlanRequest("Plano de verão", "Menos sódio.", new DateOnly(2026, 11, 1), new DateOnly(2027, 1, 31)),
            TestContext.Current.CancellationToken);

        Assert.Equal(ManageNutritionPlanResult.Done, result);
        Assert.Equal("Plano de verão", plan.Name);
        Assert.Equal(new DateOnly(2027, 1, 31), plan.EndDate);
        Assert.Equal(1, _unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task Should_Not_Activate_A_Plan_That_Is_Not_A_Draft()
    {
        var plan = AddPlan(active: true);

        var result = await CreateUseCase().ActivateAsync(_care.Nutritionist.Id, plan.Id, TestContext.Current.CancellationToken);

        Assert.Equal(ManageNutritionPlanResult.InvalidState, result);
        Assert.Equal(0, _unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task Should_Not_Finish_A_Draft()
    {
        var plan = AddPlan();

        var result = await CreateUseCase().FinishAsync(_care.Nutritionist.Id, plan.Id, TestContext.Current.CancellationToken);

        Assert.Equal(ManageNutritionPlanResult.InvalidState, result);
        Assert.Equal(NutritionPlanStatus.Draft, plan.Status);
    }

    [Fact]
    public async Task Should_Cancel_A_Draft()
    {
        var plan = AddPlan();

        var result = await CreateUseCase().CancelAsync(_care.Nutritionist.Id, plan.Id, TestContext.Current.CancellationToken);

        Assert.Equal(ManageNutritionPlanResult.Done, result);
        Assert.Equal(NutritionPlanStatus.Cancelled, plan.Status);
    }

    [Fact]
    public async Task Should_Return_Not_Found_For_Unknown_Plan()
    {
        var result = await CreateUseCase().CancelAsync(_care.Nutritionist.Id, Guid.NewGuid(), TestContext.Current.CancellationToken);

        Assert.Equal(ManageNutritionPlanResult.NotFound, result);
    }

    [Fact]
    public async Task Should_Set_And_Clear_The_Target()
    {
        var plan = AddPlan();
        var useCase = CreateUseCase();

        var set = await useCase.SetTargetAsync(
            _care.Nutritionist.Id, plan.Id, new ManageNutritionPlanUseCase.PlanTargetRequest(2000m, 150m, null, 60m),
            TestContext.Current.CancellationToken);

        Assert.Equal(ManageNutritionPlanResult.Done, set);
        Assert.Equal(2000m, plan.Target!.EnergyKcal);
        Assert.Null(plan.Target.CarbohydrateGrams);

        var cleared = await useCase.ClearTargetAsync(_care.Nutritionist.Id, plan.Id, TestContext.Current.CancellationToken);

        Assert.Equal(ManageNutritionPlanResult.Done, cleared);
        Assert.Null(plan.Target);
    }

    [Fact]
    public async Task Should_Not_Set_Target_On_Finished_Plan()
    {
        var plan = AddPlan(active: true);
        plan.Finish(new DateOnly(2026, 10, 20));

        var result = await CreateUseCase().SetTargetAsync(
            _care.Nutritionist.Id, plan.Id, new ManageNutritionPlanUseCase.PlanTargetRequest(2000m, null, null, null),
            TestContext.Current.CancellationToken);

        Assert.Equal(ManageNutritionPlanResult.InvalidState, result);
        Assert.Null(plan.Target);
    }

    // As regras abaixo também estão nos validadores. Se uma requisição passar por eles (ou se o caso de uso
    // for chamado por outro caminho), o domínio ainda recusa: a DomainException sai do caso de uso, nada é salvo,
    // e o tratador global da Fase 22 responde 422 com a mensagem do domínio.
    [Fact]
    public async Task Impossible_Target_Should_Reach_The_Handler_As_A_Domain_Rule()
    {
        var plan = AddPlan();

        var exception = await Assert.ThrowsAsync<DomainException>(() => CreateUseCase().SetTargetAsync(
            _care.Nutritionist.Id, plan.Id, new ManageNutritionPlanUseCase.PlanTargetRequest(0m, null, null, null),
            TestContext.Current.CancellationToken));

        Assert.StartsWith("A meta de calorias precisa estar entre 1 e", exception.Message);
        Assert.Equal(0, _unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task End_Date_Before_Start_Should_Reach_The_Handler_As_A_Domain_Rule()
    {
        var plan = AddPlan();

        var exception = await Assert.ThrowsAsync<DomainException>(() => CreateUseCase().UpdateAsync(
            _care.Nutritionist.Id, plan.Id,
            new UpdateNutritionPlanRequest("Plano", null, new DateOnly(2026, 11, 1), new DateOnly(2026, 10, 1)),
            TestContext.Current.CancellationToken));

        Assert.Equal("A data de fim não pode ser anterior à data de início.", exception.Message);
        Assert.Equal(0, _unitOfWork.SaveChangesCount);
    }
}
