using NogVita.Application.NutritionPlans;
using NogVita.Domain.Foods;
using NogVita.Domain.NutritionPlans;

namespace NogVita.UnitTests.Application.NutritionPlans;

// D-V2-36: as alternativas vêm dentro do item principal, com a diferença, e ficam fora dos totais.
public class SubstituteResponsesTests
{
    private static MealFoodResponse Item(Guid id, decimal kcal, decimal protein, Guid? substituteFor = null) =>
        new MealFoodResponse(id, Guid.NewGuid(), 100m, "Alimento", null, FoodSource.Taco, "1", DateTime.UtcNow,
            kcal, protein, 0m, 1m, 0m, 0m)
        { SubstituteForId = substituteFor };

    private static NutritionPlanResponse Plan(bool patientSeesMacros) =>
        new(Guid.NewGuid(), Guid.NewGuid(), "Paciente", Guid.NewGuid(), "Nutri", "Plano", null,
            new DateOnly(2026, 11, 1), null, NutritionPlanStatus.Active, DateTime.UtcNow, null, null, null, null, patientSeesMacros);

    private static IReadOnlyList<MealFoodResponse> ChickenOrBeef()
    {
        var chicken = Guid.NewGuid();
        return MealFoodResponse.GroupSubstitutes([Item(chicken, 160m, 30m), Item(Guid.NewGuid(), 190m, 27m, chicken)]);
    }

    [Fact]
    public void Substitutes_Should_Be_Nested_With_The_Difference()
    {
        var main = Assert.Single(ChickenOrBeef());

        var beef = Assert.Single(main.Substitutes);
        Assert.Null(main.Difference);
        Assert.Equal(30m, beef.Difference!.EnergyKcal);
        Assert.Equal(-3m, beef.Difference.Protein);
    }

    [Fact]
    public void Totals_Should_Count_Only_The_Main_Items()
    {
        var meal = new MealResponse(Guid.NewGuid(), "Almoço", null, 1, null, ChickenOrBeef());

        Assert.Equal(160m, meal.Totals.EnergyKcal.Value);
    }

    // D-V2-25: sem os macros para o paciente, também nas alternativas e na diferença.
    [Fact]
    public void Patient_Without_Macros_Should_Not_See_Them_In_Substitutes()
    {
        var detail = new NutritionPlanDetailResponse(Plan(patientSeesMacros: false), [new MealResponse(Guid.NewGuid(), "Almoço", null, 1, null, ChickenOrBeef())]);

        var beef = detail.ForPatient().Meals[0].Foods[0].Substitutes[0];

        Assert.Null(beef.ProteinPer100g);
        Assert.Null(beef.Difference!.Protein);
        Assert.Equal(30m, beef.Difference.EnergyKcal);
    }
}
