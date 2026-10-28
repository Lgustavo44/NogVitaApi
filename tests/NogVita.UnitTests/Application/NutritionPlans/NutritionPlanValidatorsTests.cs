using FluentValidation.TestHelper;
using NogVita.Application.NutritionPlans;
using NogVita.Domain.NutritionPlans;

namespace NogVita.UnitTests.Application.NutritionPlans;

public class NutritionPlanValidatorsTests
{
    private static readonly DateOnly Start = new(2026, 11, 1);

    [Fact]
    public void Create_Should_Accept_A_Valid_Plan()
    {
        new CreateNutritionPlanRequestValidator()
            .TestValidate(new CreateNutritionPlanRequest(Guid.NewGuid(), "Plano", null, Start, Start.AddDays(30)))
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Create_Should_Refuse_Missing_Patient_And_Name()
    {
        var result = new CreateNutritionPlanRequestValidator()
            .TestValidate(new CreateNutritionPlanRequest(Guid.Empty, "", null, Start, null));

        result.ShouldHaveValidationErrorFor(r => r.PatientId);
        result.ShouldHaveValidationErrorFor(r => r.Name);
    }

    [Fact]
    public void Create_And_Update_Should_Refuse_End_Before_Start()
    {
        new CreateNutritionPlanRequestValidator()
            .TestValidate(new CreateNutritionPlanRequest(Guid.NewGuid(), "Plano", null, Start, Start.AddDays(-1)))
            .ShouldHaveValidationErrorFor(r => r.EndDate)
            .WithErrorMessage("A data de fim não pode ser anterior à data de início.");

        new UpdateNutritionPlanRequestValidator()
            .TestValidate(new UpdateNutritionPlanRequest("Plano", null, Start, Start.AddDays(-1)))
            .ShouldHaveValidationErrorFor(r => r.EndDate);
    }

    [Fact]
    public void Update_Should_Respect_Name_And_Description_Limits()
    {
        var result = new UpdateNutritionPlanRequestValidator().TestValidate(new UpdateNutritionPlanRequest(
            new string('a', NutritionPlan.NameMaxLength + 1), new string('a', NutritionPlan.DescriptionMaxLength + 1), Start, null));

        result.ShouldHaveValidationErrorFor(r => r.Name);
        result.ShouldHaveValidationErrorFor(r => r.Description);
    }

    [Fact]
    public void Meal_Should_Need_A_Name_Within_Limits()
    {
        var validator = new MealRequestValidator();

        validator.TestValidate(new MealRequest("Almoço", null, new TimeOnly(12, 0))).ShouldNotHaveAnyValidationErrors();
        validator.TestValidate(new MealRequest("", null)).ShouldHaveValidationErrorFor(r => r.Name);
        validator.TestValidate(new MealRequest(new string('a', Meal.NameMaxLength + 1), null)).ShouldHaveValidationErrorFor(r => r.Name);
        validator.TestValidate(new MealRequest("Almoço", new string('a', Meal.NotesMaxLength + 1))).ShouldHaveValidationErrorFor(r => r.Notes);
    }

    [Fact]
    public void Reorder_Should_Need_The_Meals()
    {
        new ReorderMealsRequestValidator().TestValidate(new ReorderMealsRequest([Guid.NewGuid()])).ShouldNotHaveAnyValidationErrors();
        new ReorderMealsRequestValidator().TestValidate(new ReorderMealsRequest([])).ShouldHaveValidationErrorFor(r => r.MealIds);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    [InlineData(2000.01)]
    public void Food_Quantity_Should_Be_Positive_And_Within_The_Limit(decimal quantity)
    {
        new AddMealFoodRequestValidator().TestValidate(new AddMealFoodRequest(Guid.NewGuid(), quantity))
            .ShouldHaveValidationErrorFor(r => r.QuantityInGrams);
        new UpdateMealFoodRequestValidator().TestValidate(new UpdateMealFoodRequest(quantity))
            .ShouldHaveValidationErrorFor(r => r.QuantityInGrams);
    }

    [Fact]
    public void Food_Should_Accept_The_Maximum_Quantity()
    {
        new AddMealFoodRequestValidator().TestValidate(new AddMealFoodRequest(Guid.NewGuid(), MealFood.MaxQuantityInGrams))
            .ShouldNotHaveAnyValidationErrors();
        new AddMealFoodRequestValidator().TestValidate(new AddMealFoodRequest(Guid.Empty, 100m))
            .ShouldHaveValidationErrorFor(r => r.FoodId);
    }

    [Fact]
    public void Plan_List_Should_Refuse_Unknown_Status()
    {
        var validator = new NutritionPlanListRequestValidator();

        validator.TestValidate(new NutritionPlanListRequest { Status = NutritionPlanStatus.Active }).ShouldNotHaveAnyValidationErrors();
        validator.TestValidate(new NutritionPlanListRequest { Status = (NutritionPlanStatus)99 }).ShouldHaveValidationErrorFor(r => r.Status);
    }

    [Fact]
    public void Target_Should_Refuse_Impossible_Values()
    {
        var validator = new ManageNutritionPlanUseCase.PlanTargetRequestValidator();

        validator.TestValidate(new ManageNutritionPlanUseCase.PlanTargetRequest(2000m, 150m, 250m, 60m)).ShouldNotHaveAnyValidationErrors();
        validator.TestValidate(new ManageNutritionPlanUseCase.PlanTargetRequest(0m, null, null, null)).ShouldHaveValidationErrorFor(r => r.EnergyKcal);
        validator.TestValidate(new ManageNutritionPlanUseCase.PlanTargetRequest(PlanTarget.MaxEnergyKcal + 1, null, null, null)).ShouldHaveValidationErrorFor(r => r.EnergyKcal);
        validator.TestValidate(new ManageNutritionPlanUseCase.PlanTargetRequest(2000m, -1m, null, null)).ShouldHaveValidationErrorFor(r => r.ProteinGrams);
    }
}
