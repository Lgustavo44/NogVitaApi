using System.Net;
using System.Net.Http.Json;
using NogVita.IntegrationTests.Infrastructure;

namespace NogVita.IntegrationTests.Authorization;

// Etapa 3, a segunda metade: ter o papel não basta. O nutricionista só alcança os pacientes que acompanha (RN12),
// e perde o acesso quando o acompanhamento termina (RN13). Fora disso, a resposta é 404, e não 403:
// a API não confirma nem que o paciente existe.
public class CareAccessTests(NogVitaApiFixture fixture)
{
    private readonly TestAccounts _accounts = new(fixture);
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task Nutritionist_Should_Not_Reach_A_Patient_He_Does_Not_Follow()
    {
        var patient = await _accounts.NewPatientAsync();
        await _accounts.FollowAsync(patient, await _accounts.NewNutritionistAsync());
        var stranger = _accounts.As(await _accounts.NewNutritionistAsync());

        Assert.Equal(HttpStatusCode.NotFound, (await stranger.GetAsync($"/api/v1/nutritionists/me/patients/{patient.Id}", _ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await stranger.GetAsync($"/api/v1/nutritionists/me/patients/{patient.Id}/nutrition-plans", _ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await stranger.GetAsync($"/api/v1/nutritionists/me/patients/{patient.Id}/weight-records", _ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await stranger.PostAsJsonAsync(
            $"/api/v1/nutritionists/me/patients/{patient.Id}/weight-records", new { weightKg = 70m }, TestAccounts.Json, _ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await stranger.PostAsJsonAsync("/api/v1/nutritionists/me/nutrition-plans", new
        {
            patientId = patient.Id,
            name = "Plano intruso",
            startDate = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd")
        }, TestAccounts.Json, _ct)).StatusCode);
    }

    [Fact]
    public async Task Nutritionist_Should_Not_Reach_The_Plan_Of_Another_Nutritionist()
    {
        var patient = await _accounts.NewPatientAsync();
        var owner = await _accounts.NewNutritionistAsync();
        await _accounts.FollowAsync(patient, owner);
        var planId = await _accounts.NewPlanAsync(owner, patient.Id);
        var stranger = _accounts.As(await _accounts.NewNutritionistAsync());

        Assert.Equal(HttpStatusCode.OK, (await _accounts.As(owner).GetAsync($"/api/v1/nutritionists/me/nutrition-plans/{planId}", _ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await stranger.GetAsync($"/api/v1/nutritionists/me/nutrition-plans/{planId}", _ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await stranger.PostAsync($"/api/v1/nutritionists/me/nutrition-plans/{planId}/activate", null, _ct)).StatusCode);
    }

    [Fact]
    public async Task Patient_Should_Not_See_The_Plan_Of_Another_Patient()
    {
        var patient = await _accounts.NewPatientAsync();
        var nutritionist = await _accounts.NewNutritionistAsync();
        await _accounts.FollowAsync(patient, nutritionist);
        var planId = await _accounts.NewPlanAsync(nutritionist, patient.Id, activate: true);
        var otherPatient = _accounts.As(await _accounts.NewPatientAsync());

        Assert.Equal(HttpStatusCode.OK, (await _accounts.As(patient).GetAsync($"/api/v1/patients/me/nutrition-plans/{planId}", _ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await otherPatient.GetAsync($"/api/v1/patients/me/nutrition-plans/{planId}", _ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await otherPatient.GetAsync($"/api/v1/patients/me/nutrition-plans/{planId}/pdf", _ct)).StatusCode);
    }

    // RN13: encerrado o acompanhamento, o nutricionista perde o acesso; o paciente continua vendo o plano.
    [Fact]
    public async Task Ending_The_Follow_Up_Should_Cut_The_Nutritionist_Access()
    {
        var patient = await _accounts.NewPatientAsync();
        var nutritionist = await _accounts.NewNutritionistAsync();
        await _accounts.FollowAsync(patient, nutritionist);
        var planId = await _accounts.NewPlanAsync(nutritionist, patient.Id, activate: true);
        var nutritionistClient = _accounts.As(nutritionist);
        Assert.Equal(HttpStatusCode.OK, (await nutritionistClient.GetAsync($"/api/v1/nutritionists/me/patients/{patient.Id}", _ct)).StatusCode);

        var end = await _accounts.As(patient).PostAsync("/api/v1/patients/me/care-relationship/end", null, _ct);
        Assert.Equal(HttpStatusCode.NoContent, end.StatusCode);

        Assert.Equal(HttpStatusCode.NotFound, (await nutritionistClient.GetAsync($"/api/v1/nutritionists/me/patients/{patient.Id}", _ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await nutritionistClient.GetAsync($"/api/v1/nutritionists/me/nutrition-plans/{planId}", _ct)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _accounts.As(patient).GetAsync($"/api/v1/patients/me/nutrition-plans/{planId}", _ct)).StatusCode);
    }
}
