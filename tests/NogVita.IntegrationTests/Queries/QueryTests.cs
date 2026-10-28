using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NogVita.Domain.Foods;
using NogVita.IntegrationTests.Infrastructure;

namespace NogVita.IntegrationTests.Queries;

// Etapa 4: o que só o Postgres de verdade mostra. O unaccent, o ILIKE, os índices únicos parciais,
// os filtros dentro das consultas do EF e a ordenação feita pelo banco.
public class QueryTests(NogVitaApiFixture fixture)
{
    private readonly TestAccounts _accounts = new(fixture);
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task Food_Search_Should_Ignore_Accents_And_Case()
    {
        var nutritionist = await _accounts.NewNutritionistAsync();

        var names = await SearchFoodsAsync(_accounts.As(nutritionist), "/api/v1/foods?search=ACUCAR&pageSize=100");

        Assert.Contains("Açúcar, cristal", names);
        Assert.Contains("Açúcar, mascavo", names);
    }

    // Fase 20: cada palavra precisa aparecer, em qualquer ordem e posição.
    [Fact]
    public async Task Food_Search_Should_Match_Every_Word_In_Any_Order()
    {
        var nutritionist = await _accounts.NewNutritionistAsync();

        var names = await SearchFoodsAsync(_accounts.As(nutritionist), "/api/v1/foods?search=integral%20arroz&pageSize=100");

        Assert.Contains("Arroz, integral, cozido", names);
        Assert.All(names, name =>
        {
            Assert.Contains("arroz", name, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("integral", name, StringComparison.OrdinalIgnoreCase);
        });
    }

    // O % e o _ são curingas do LIKE. Sem o escape, "%%" acharia todos os 597 alimentos da TACO, e "__" quase todos.
    [Theory]
    [InlineData("%25%25")]
    [InlineData("__")]
    public async Task Food_Search_Should_Treat_Like_Wildcards_As_Plain_Text(string search)
    {
        var nutritionist = await _accounts.NewNutritionistAsync();

        var page = await _accounts.As(nutritionist).GetFromJsonAsync<JsonElement>($"/api/v1/foods?search={search}", TestAccounts.Json, _ct);

        Assert.Equal(0, page.GetProperty("totalItems").GetInt32());
    }

    // Fase 15 e Fase 22: o alimento desativado some da busca do nutricionista, mas continua na do admin.
    [Fact]
    public async Task Inactive_Food_Should_Only_Appear_In_The_Admin_Search()
    {
        var name = $"Alimento de teste {Guid.NewGuid():N}";
        Guid foodId;
        await using (var context = fixture.CreateDbContext())
        {
            var food = Food.FromTaco(Random.Shared.Next(100_000, int.MaxValue), name, null,
                new NutrientsPer100g(100m, 1m, 1m, 1m, 1m, 1m));
            context.Foods.Add(food);
            await context.SaveChangesAsync(_ct);
            foodId = food.Id;
        }

        var admin = _accounts.As(await _accounts.AdminAsync());
        var nutritionist = _accounts.As(await _accounts.NewNutritionistAsync());
        Assert.Equal(HttpStatusCode.NoContent, (await admin.PostAsync($"/api/v1/admin/foods/{foodId}/deactivate", null, _ct)).StatusCode);

        var search = $"?search={Uri.EscapeDataString(name)}";
        Assert.Empty(await SearchFoodsAsync(nutritionist, "/api/v1/foods" + search));
        Assert.Equal([name], await SearchFoodsAsync(admin, "/api/v1/admin/foods" + search));
    }

    // Fase 22: o índice único é a última barreira contra o clique duplo. Nunca 500, e nunca dois registros.
    [Fact]
    public async Task Concurrent_Weight_Records_Should_Leave_A_Single_Row_And_No_500()
    {
        var patient = await _accounts.NewPatientAsync();
        var client = _accounts.As(patient);
        var body = new { weightKg = 70.5m, recordedOn = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-3)).ToString("yyyy-MM-dd") };

        var responses = await Task.WhenAll(Enumerable.Range(0, 10).Select(_ =>
            client.PostAsJsonAsync("/api/v1/patients/me/weight-records", body, TestAccounts.Json, _ct)));

        Assert.All(responses, r => Assert.Contains(r.StatusCode, new[] { HttpStatusCode.Created, HttpStatusCode.OK, HttpStatusCode.Conflict }));
        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Created);

        await using var context = fixture.CreateDbContext();
        Assert.Equal(1, await context.WeightRecords.CountAsync(w => w.PatientId == patient.Id, _ct));
    }

    // Um paciente tem no máximo um acompanhamento ativo (o índice parcial ux_care_relationship_patient_active).
    [Fact]
    public async Task Patient_Should_Not_Have_Two_Active_Follow_Ups()
    {
        var patient = await _accounts.NewPatientAsync();
        await _accounts.FollowAsync(patient, await _accounts.NewNutritionistAsync());
        var other = await _accounts.NewNutritionistAsync();

        var second = await _accounts.As(patient).PostAsJsonAsync(
            "/api/v1/patients/me/nutritionist-requests", new { nutritionistId = other.Id }, TestAccounts.Json, _ct);

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        await using var context = fixture.CreateDbContext();
        Assert.Equal(1, await context.CareRelationships.CountAsync(c => c.PatientId == patient.Id && c.EndedAtUtc == null, _ct));
    }

    // D163: o filtro do paciente bloqueado está dentro da consulta do EF (o "where" no join), e não na memória.
    [Fact]
    public async Task Patient_Blocked_By_Admin_Should_Leave_The_Nutritionist_List()
    {
        var patient = await _accounts.NewPatientAsync();
        var nutritionist = await _accounts.NewNutritionistAsync();
        await _accounts.FollowAsync(patient, nutritionist);
        var nutritionistClient = _accounts.As(nutritionist);

        Assert.Equal([patient.Id], await MyPatientIdsAsync(nutritionistClient));

        var admin = _accounts.As(await _accounts.AdminAsync());
        await admin.PostAsync($"/api/v1/admin/users/{patient.Id}/deactivate", null, _ct);

        Assert.Empty(await MyPatientIdsAsync(nutritionistClient));
        Assert.Equal(HttpStatusCode.NotFound,
            (await nutritionistClient.GetAsync($"/api/v1/nutritionists/me/patients/{patient.Id}", _ct)).StatusCode);
    }

    // A ordenação e o filtro de datas rodam no banco: do mais recente para o mais antigo, dentro do intervalo.
    [Fact]
    public async Task Weight_History_Should_Be_Filtered_And_Ordered_By_The_Database()
    {
        var patient = await _accounts.NewPatientAsync();
        var client = _accounts.As(patient);
        var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(-3));

        foreach (var (daysAgo, weight) in new[] { (30, 72m), (10, 71m), (20, 71.5m), (2, 70m) })
        {
            var response = await client.PostAsJsonAsync("/api/v1/patients/me/weight-records",
                new { weightKg = weight, recordedOn = today.AddDays(-daysAgo).ToString("yyyy-MM-dd") }, TestAccounts.Json, _ct);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        var from = today.AddDays(-25).ToString("yyyy-MM-dd");
        var to = today.AddDays(-5).ToString("yyyy-MM-dd");
        var page = await client.GetFromJsonAsync<JsonElement>($"/api/v1/patients/me/weight-records?from={from}&to={to}", TestAccounts.Json, _ct);

        var weights = page.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("weightKg").GetDecimal()).ToList();
        Assert.Equal([71m, 71.5m], weights); // 10 e 20 dias atrás; os de 30 e 2 dias ficam fora
        Assert.Equal(2, page.GetProperty("totalItems").GetInt32());
    }

    private async Task<List<string>> SearchFoodsAsync(HttpClient client, string url)
    {
        var page = await client.GetFromJsonAsync<JsonElement>(url, TestAccounts.Json, _ct);
        return page.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("name").GetString()!).ToList();
    }

    private async Task<List<Guid>> MyPatientIdsAsync(HttpClient nutritionist)
    {
        var page = await nutritionist.GetFromJsonAsync<JsonElement>("/api/v1/nutritionists/me/patients", TestAccounts.Json, _ct);
        return page.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("patientId").GetGuid()).ToList();
    }
}
