using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NogVita.Domain.Foods;
using NogVita.IntegrationTests.Infrastructure;

namespace NogVita.IntegrationTests.Queries;

// Etapa 4: um plano completo, montado pela API com alimentos da TACO de verdade, e o cálculo conferido
// contra os valores que a própria TACO gravou no banco (a porção é proporcional, e só o total é arredondado).
public class PlanCalculationTests(NogVitaApiFixture fixture)
{
    private readonly TestAccounts _accounts = new(fixture);
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task Plan_Totals_Should_Match_The_Taco_Values()
    {
        var rice = await TacoFoodAsync("Arroz, tipo 1, cozido");
        var beans = await TacoFoodAsync("Feijão, carioca, cozido");

        var patient = await _accounts.NewPatientAsync();
        var nutritionist = await _accounts.NewNutritionistAsync();
        await _accounts.FollowAsync(patient, nutritionist);
        var planId = await _accounts.NewPlanAsync(nutritionist, patient.Id);

        var lunch = await _accounts.NewMealAsync(nutritionist, planId, "Almoço");
        await _accounts.AddFoodAsync(nutritionist, planId, lunch, rice.Id, 150m);
        await _accounts.AddFoodAsync(nutritionist, planId, lunch, beans.Id, 80m);

        var dinner = await _accounts.NewMealAsync(nutritionist, planId, "Jantar");
        await _accounts.AddFoodAsync(nutritionist, planId, dinner, rice.Id, 100m);

        var detail = await _accounts.As(nutritionist).GetFromJsonAsync<JsonElement>(
            $"/api/v1/nutritionists/me/nutrition-plans/{planId}", TestAccounts.Json, _ct);

        var meals = detail.GetProperty("meals").EnumerateArray().ToList();
        Assert.Equal(["Almoço", "Jantar"], meals.Select(m => m.GetProperty("name").GetString()));

        // Almoço: 150 g de arroz e 80 g de feijão. Jantar: 100 g de arroz. O dia: os dois somados.
        AssertTotals(meals[0].GetProperty("totals"), (rice, 150m), (beans, 80m));
        AssertTotals(meals[1].GetProperty("totals"), (rice, 100m));
        AssertTotals(detail.GetProperty("dailyTotals"), (rice, 150m), (beans, 80m), (rice, 100m));
    }

    private static void AssertTotals(JsonElement totals, params (Food Food, decimal Grams)[] portions)
    {
        AssertTotal(totals, "energyKcal", portions.Sum(p => p.Food.Nutrients.EnergyKcal!.Value * p.Grams / 100m));
        AssertTotal(totals, "protein", portions.Sum(p => p.Food.Nutrients.Protein!.Value * p.Grams / 100m));
        AssertTotal(totals, "carbohydrate", portions.Sum(p => p.Food.Nutrients.Carbohydrate!.Value * p.Grams / 100m));
        AssertTotal(totals, "fat", portions.Sum(p => p.Food.Nutrients.Fat!.Value * p.Grams / 100m));
    }

    private static void AssertTotal(JsonElement totals, string nutrient, decimal rawExpected)
    {
        var total = totals.GetProperty(nutrient);
        Assert.Equal(Math.Round(rawExpected, 1, MidpointRounding.AwayFromZero), total.GetProperty("value").GetDecimal());
        Assert.True(total.GetProperty("isComplete").GetBoolean(), $"{nutrient} deveria estar completo");
    }

    private async Task<Food> TacoFoodAsync(string name)
    {
        await using var context = fixture.CreateDbContext();
        return await context.Foods.SingleAsync(f => f.Source == FoodSource.Taco && f.Name == name, _ct);
    }
}
