using NogVita.Domain.Common;
using NogVita.Domain.NutritionPlans;

namespace NogVita.UnitTests.Domain.NutritionPlans;

public class NutritionPlanTests
{
    private static readonly Guid PatientId = Guid.NewGuid();
    private static readonly Guid NutritionistId = Guid.NewGuid();
    private static readonly DateOnly Start = new(2026, 10, 1);

    private static NutritionPlan CreateDraft(DateOnly? endDate = null) =>
        new(PatientId, NutritionistId, "  Hipertrofia  ", "  Plano de ganho de massa.  ", Start, endDate);

    [Fact]
    public void Should_Start_As_Draft_With_Normalized_Text()
    {
        var plan = CreateDraft();

        Assert.Equal(NutritionPlanStatus.Draft, plan.Status);
        Assert.Equal("Hipertrofia", plan.Name);
        Assert.Equal("Plano de ganho de massa.", plan.Description);
        Assert.True(plan.IsEditable);
    }

    [Fact]
    public void Should_Not_Create_Plan_For_Self()
    {
        Assert.Throws<DomainException>(() => new NutritionPlan(PatientId, PatientId, "Plano", null, Start, null));
    }

    [Fact]
    public void Should_Reject_End_Before_Start()
    {
        Assert.Throws<DomainException>(() => CreateDraft(endDate: Start.AddDays(-1)));
    }

    [Fact]
    public void Should_Accept_End_Equal_To_Start()
    {
        var plan = CreateDraft(endDate: Start);

        Assert.Equal(Start, plan.EndDate);
    }

    [Fact]
    public void Should_Follow_Draft_Active_Finished()
    {
        var plan = CreateDraft();

        plan.Activate();
        Assert.Equal(NutritionPlanStatus.Active, plan.Status);

        plan.Finish(Start.AddDays(30));
        Assert.Equal(NutritionPlanStatus.Finished, plan.Status);
        Assert.Equal(Start.AddDays(30), plan.EndDate);
        Assert.False(plan.IsEditable);
    }

    [Fact]
    public void Should_Cancel_Only_Drafts()
    {
        var draft = CreateDraft();
        draft.Cancel();
        Assert.Equal(NutritionPlanStatus.Cancelled, draft.Status);

        var active = CreateDraft();
        active.Activate();
        Assert.Throws<DomainException>(() => active.Cancel());
    }

    [Fact]
    public void Should_Not_Activate_Cancelled_Plan()
    {
        var plan = CreateDraft();
        plan.Cancel();

        Assert.Throws<DomainException>(() => plan.Activate());
    }

    [Fact]
    public void Should_Not_Update_Finished_Plan()
    {
        var plan = CreateDraft();
        plan.Activate();
        plan.Finish(Start.AddDays(10));

        Assert.Throws<DomainException>(() => plan.UpdateDetails("Novo nome", null, Start, null));
    }

    [Fact]
    public void Should_Shorten_Future_End_Date_When_Finished_Early()
    {
        var plan = CreateDraft(endDate: Start.AddDays(90));
        plan.Activate();

        plan.Finish(Start.AddDays(20));

        Assert.Equal(Start.AddDays(20), plan.EndDate);
    }

    [Fact]
    public void Should_Keep_Past_End_Date_When_Finished_Late()
    {
        var plan = CreateDraft(endDate: Start.AddDays(30));
        plan.Activate();

        plan.Finish(Start.AddDays(45));

        Assert.Equal(Start.AddDays(30), plan.EndDate);
    }

    [Fact]
    public void Should_Never_End_Before_Start_When_Finished_Before_Starting()
    {
        var plan = CreateDraft();
        plan.Activate();

        plan.Finish(Start.AddDays(-5));

        Assert.Equal(Start, plan.EndDate);
    }
}