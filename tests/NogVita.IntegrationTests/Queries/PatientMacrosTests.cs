using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NogVita.Domain.Foods;
using NogVita.IntegrationTests.Infrastructure;

namespace NogVita.IntegrationTests.Queries;

// D-V2-23 a D-V2-26: o paciente só recebe proteína, carboidrato e gordura quando o nutricionista libera.
public class PatientMacrosTests(NogVitaApiFixture fixture)
{
    private readonly TestAccounts _accounts = new(fixture);
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task Patient_Should_Only_Receive_Macros_When_The_Nutritionist_Releases_Them()
    {
        Guid riceId;
        await using (var context = fixture.CreateDbContext())
        {
            riceId = (await context.Foods.SingleAsync(f => f.Source == FoodSource.Taco && f.Name == "Arroz, tipo 1, cozido", _ct)).Id;
        }

        var patient = await _accounts.NewPatientAsync();
        var nutritionist = await _accounts.NewNutritionistAsync();
        await _accounts.FollowAsync(patient, nutritionist);
        var planId = await _accounts.NewPlanAsync(nutritionist, patient.Id);
        var lunch = await _accounts.NewMealAsync(nutritionist, planId, "Almoço");
        await _accounts.AddFoodAsync(nutritionist, planId, lunch, riceId, 150m);

        var nutritionistClient = _accounts.As(nutritionist);
        var target = await nutritionistClient.PutAsJsonAsync(
            $"/api/v1/nutritionists/me/nutrition-plans/{planId}/target", new { energyKcal = 2000m, proteinGrams = 150m }, TestAccounts.Json, _ct);
        Assert.Equal(HttpStatusCode.NoContent, target.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await nutritionistClient.PostAsync($"/api/v1/nutritionists/me/nutrition-plans/{planId}/activate", null, _ct)).StatusCode);

        // O padrão: chave desligada. O paciente fica com as kcal e sem os macros.
        var hidden = await PatientPlanAsync(patient);
        Assert.False(hidden.GetProperty("macrosVisible").GetBoolean());
        Assert.Equal(JsonValueKind.Null, hidden.GetProperty("dailyTotals").GetProperty("protein").ValueKind);
        Assert.True(hidden.GetProperty("dailyTotals").GetProperty("energyKcal").GetProperty("value").GetDecimal() > 0);
        var food = hidden.GetProperty("meals")[0].GetProperty("foods")[0];
        Assert.Equal(JsonValueKind.Null, food.GetProperty("proteinPer100g").ValueKind);
        Assert.Equal(JsonValueKind.Null, hidden.GetProperty("plan").GetProperty("targetProteinGrams").ValueKind);
        Assert.Equal(JsonValueKind.Null, hidden.GetProperty("target").GetProperty("protein").ValueKind);

        var list = await _accounts.As(patient).GetFromJsonAsync<JsonElement>("/api/v1/patients/me/nutrition-plans", TestAccounts.Json, _ct);
        Assert.Equal(JsonValueKind.Null, list.GetProperty("items")[0].GetProperty("targetProteinGrams").ValueKind);

        // O mesmo plano, visto pelo nutricionista: tudo.
        var full = await nutritionistClient.GetFromJsonAsync<JsonElement>($"/api/v1/nutritionists/me/nutrition-plans/{planId}", TestAccounts.Json, _ct);
        Assert.True(full.GetProperty("macrosVisible").GetBoolean());
        Assert.False(full.GetProperty("plan").GetProperty("patientSeesMacros").GetBoolean());
        Assert.True(full.GetProperty("dailyTotals").GetProperty("protein").GetProperty("value").GetDecimal() > 0);

        // A chave vale até num plano finalizado (D-V2-26).
        Assert.Equal(HttpStatusCode.NoContent, (await nutritionistClient.PostAsync($"/api/v1/nutritionists/me/nutrition-plans/{planId}/finish", null, _ct)).StatusCode);
        var released = await nutritionistClient.PutAsJsonAsync(
            $"/api/v1/nutritionists/me/nutrition-plans/{planId}/patient-view", new { patientSeesMacros = true }, TestAccounts.Json, _ct);
        Assert.Equal(HttpStatusCode.NoContent, released.StatusCode);

        var visible = await _accounts.As(patient).GetFromJsonAsync<JsonElement>($"/api/v1/patients/me/nutrition-plans/{planId}", TestAccounts.Json, _ct);
        Assert.True(visible.GetProperty("macrosVisible").GetBoolean());
        Assert.True(visible.GetProperty("dailyTotals").GetProperty("protein").GetProperty("value").GetDecimal() > 0);
        Assert.Equal(150m, visible.GetProperty("plan").GetProperty("targetProteinGrams").GetDecimal());
    }

    private Task<JsonElement> PatientPlanAsync(TestAccount patient) =>
        _accounts.As(patient).GetFromJsonAsync<JsonElement>("/api/v1/patients/me/nutrition-plans/current", TestAccounts.Json, _ct);
}
