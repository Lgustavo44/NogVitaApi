using NogVita.Domain.Common;
using NogVita.Domain.NutritionPlans;

namespace NogVita.UnitTests.Domain.NutritionPlans;

public class PlanTargetTests
{
    private static NutritionPlan CreatePlan() =>
        new(Guid.NewGuid(), Guid.NewGuid(), "Plano", null, new DateOnly(2026, 10, 1), null);

    [Fact]
    public void Should_Accept_Calories_Only()
    {
        var target = new PlanTarget(2000m, null, null, null);

        Assert.Equal(2000m, target.EnergyKcal);
        Assert.Null(target.ProteinGrams);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    [InlineData(10001)]
    public void Should_Reject_Invalid_Calories(double kcal)
    {
        Assert.Throws<DomainException>(() => new PlanTarget((decimal)kcal, null, null, null));
    }

    [Fact]
    public void Should_Reject_Negative_Macro()
    {
        Assert.Throws<DomainException>(() => new PlanTarget(2000m, -1m, null, null));
    }

    [Fact]
    public void Plan_Should_Set_And_Clear_Target()
    {
        var plan = CreatePlan();

        plan.SetTarget(new PlanTarget(2200m, 150m, 250m, 70m));
        Assert.Equal(new PlanTarget(2200m, 150m, 250m, 70m), plan.Target);

        plan.ClearTarget();
        Assert.Null(plan.Target);
    }

    [Fact]
    public void Finished_Plan_Should_Not_Change_Target()
    {
        var plan = CreatePlan();
        plan.Activate();
        plan.Finish(new DateOnly(2026, 10, 20));

        Assert.Throws<DomainException>(() => plan.SetTarget(new PlanTarget(2000m, null, null, null)));
    }
}
