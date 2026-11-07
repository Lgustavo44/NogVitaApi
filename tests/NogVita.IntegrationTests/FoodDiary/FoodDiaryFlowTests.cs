using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using NogVita.IntegrationTests.Infrastructure;

namespace NogVita.IntegrationTests.FoodDiary;

// E1 + E2 + E6.1: a porção e a alternativa no plano, o check-in e o extra do paciente, e a leitura do nutricionista,
// com os retratos indo e voltando do jsonb no Postgres.
public class FoodDiaryFlowTests(NogVitaApiFixture fixture)
{
    private readonly TestAccounts _accounts = new(fixture);
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    private static string Today => DateOnly.FromDateTime(DateTime.UtcNow.AddHours(-3)).ToString("yyyy-MM-dd");

    private async Task<JsonElement> FirstFoodAsync(HttpClient client, string search)
    {
        var page = await client.GetFromJsonAsync<JsonElement>($"/api/v1/foods?search={search}&source=Taco", TestAccounts.Json, _ct);
        return page.GetProperty("items")[0];
    }

    private async Task<Guid> PostForIdAsync(HttpClient client, string url, object body)
    {
        var response = await client.PostAsJsonAsync(url, body, TestAccounts.Json, _ct);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<JsonElement>(TestAccounts.Json, _ct)).GetProperty("id").GetGuid();
    }

    [Fact]
    public async Task Patient_Should_Check_In_With_Portions_And_The_Nutritionist_Should_Read_It()
    {
        var patient = await _accounts.NewPatientAsync();
        var nutritionist = await _accounts.NewNutritionistAsync();
        await _accounts.FollowAsync(patient, nutritionist);
        var nutri = _accounts.As(nutritionist);
        var me = _accounts.As(patient);

        // O plano: o arroz numa porção do IBGE, e o feijão como alternativa dele.
        var planId = await PostForIdAsync(nutri, "/api/v1/nutritionists/me/nutrition-plans",
            new { patientId = patient.Id, name = "Plano do diário", startDate = Today });
        var mealId = await PostForIdAsync(nutri, $"/api/v1/nutritionists/me/nutrition-plans/{planId}/meals", new { name = "Almoço" });

        var rice = await FirstFoodAsync(nutri, "arroz tipo 1 cozido");
        var spoon = rice.GetProperty("portions")[0];
        var riceItem = await PostForIdAsync(nutri, $"/api/v1/nutritionists/me/nutrition-plans/{planId}/meals/{mealId}/foods",
            new { foodId = rice.GetProperty("id").GetGuid(), portionId = spoon.GetProperty("id").GetGuid(), portionQuantity = 2 });

        var beans = await FirstFoodAsync(nutri, "feijao carioca cozido");
        await PostForIdAsync(nutri, $"/api/v1/nutritionists/me/nutrition-plans/{planId}/meals/{mealId}/foods",
            new { foodId = beans.GetProperty("id").GetGuid(), quantityInGrams = 80, substituteForId = riceItem });

        Assert.Equal(HttpStatusCode.NoContent,
            (await nutri.PostAsync($"/api/v1/nutritionists/me/nutrition-plans/{planId}/activate", null, _ct)).StatusCode);

        var detail = await me.GetFromJsonAsync<JsonElement>("/api/v1/patients/me/nutrition-plans/current", TestAccounts.Json, _ct);
        var item = detail.GetProperty("meals")[0].GetProperty("foods")[0];
        Assert.Equal(2m, item.GetProperty("measure").GetProperty("quantity").GetDecimal());
        Assert.Equal(1, item.GetProperty("substitutes").GetArrayLength());

        // O paciente: o check-in do almoço e um extra buscado na base.
        Assert.Equal(HttpStatusCode.NoContent, (await me.PutAsJsonAsync(
            $"/api/v1/patients/me/diary/{Today}/meals/{mealId}", new { adherence = "Followed", notes = "Tudo certo" }, TestAccounts.Json, _ct)).StatusCode);

        var banana = await FirstFoodAsync(me, "banana prata");
        await PostForIdAsync(me, $"/api/v1/patients/me/diary/{Today}/extras",
            new { foodId = banana.GetProperty("id").GetGuid(), mealId = (Guid?)null, quantityInGrams = 100 });

        var day = await me.GetFromJsonAsync<JsonElement>($"/api/v1/patients/me/diary/{Today}", TestAccounts.Json, _ct);
        Assert.Equal("Followed", day.GetProperty("meals")[0].GetProperty("adherence").GetString());
        Assert.Equal(1, day.GetProperty("extrasOutsideMeals").GetArrayLength());
        Assert.True(day.GetProperty("consumed").GetProperty("energyKcal").GetProperty("value").GetDecimal() > 0);

        // O check-in da semana e a devolutiva.
        var monday = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(-3));
        monday = monday.AddDays(-(((int)monday.DayOfWeek + 6) % 7));
        var week = await me.PutAsJsonAsync($"/api/v1/patients/me/weekly-check-ins/{monday:yyyy-MM-dd}",
            new { hunger = 3, energy = 4, sleep = 2, difficulties = "Fome à noite", comment = (string?)null }, TestAccounts.Json, _ct);
        Assert.Equal(HttpStatusCode.OK, week.StatusCode);
        var weekId = (await week.Content.ReadFromJsonAsync<JsonElement>(TestAccounts.Json, _ct)).GetProperty("id").GetGuid();

        var awaiting = await nutri.GetFromJsonAsync<JsonElement>("/api/v1/nutritionists/me/weekly-check-ins/awaiting-feedback", TestAccounts.Json, _ct);
        Assert.Contains(awaiting.EnumerateArray(), w => w.GetProperty("id").GetGuid() == weekId);

        Assert.Equal(HttpStatusCode.OK, (await nutri.PutAsJsonAsync(
            $"/api/v1/nutritionists/me/weekly-check-ins/{weekId}/feedback", new { feedback = "Vamos ajustar o lanche." }, TestAccounts.Json, _ct)).StatusCode);

        // O nutricionista lê o diário do paciente, com a adesão.
        var diary = await nutri.GetFromJsonAsync<JsonElement>($"/api/v1/nutritionists/me/patients/{patient.Id}/diary", TestAccounts.Json, _ct);
        Assert.Equal(1, diary.GetProperty("adherence").GetProperty("followed").GetInt32());
        Assert.Equal("Tudo certo", diary.GetProperty("days")[0].GetProperty("meals")[0].GetProperty("notes").GetString());
    }
}
