using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using NogVita.IntegrationTests.Infrastructure;

namespace NogVita.IntegrationTests.Anamneses;

// E6.6: o questionário do nutricionista, o envio, a resposta do paciente e o acesso dos dois lados,
// com as perguntas e as respostas indo e voltando do jsonb no Postgres.
public class AnamnesisFlowTests(NogVitaApiFixture fixture)
{
    private readonly TestAccounts _accounts = new(fixture);
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    private async Task<Guid> NewQuestionnaireFromTemplateAsync(HttpClient nutritionist)
    {
        var template = await nutritionist.GetFromJsonAsync<JsonElement>("/api/v1/nutritionists/me/questionnaires/template", TestAccounts.Json, _ct);
        var created = await nutritionist.PostAsJsonAsync("/api/v1/nutritionists/me/questionnaires", template, TestAccounts.Json, _ct);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        return (await created.Content.ReadFromJsonAsync<JsonElement>(TestAccounts.Json, _ct)).GetProperty("id").GetGuid();
    }

    private async Task<Guid> SendAsync(HttpClient nutritionist, Guid patientId, Guid questionnaireId)
    {
        var sent = await nutritionist.PostAsJsonAsync(
            $"/api/v1/nutritionists/me/patients/{patientId}/anamneses", new { questionnaireId }, TestAccounts.Json, _ct);
        Assert.Equal(HttpStatusCode.Created, sent.StatusCode);
        return (await sent.Content.ReadFromJsonAsync<JsonElement>(TestAccounts.Json, _ct)).GetProperty("id").GetGuid();
    }

    [Fact]
    public async Task Patient_Should_Answer_Once_And_The_Nutritionist_Should_Read_The_Answers()
    {
        var patient = await _accounts.NewPatientAsync();
        var nutritionist = await _accounts.NewNutritionistAsync();
        await _accounts.FollowAsync(patient, nutritionist);
        var nutritionistClient = _accounts.As(nutritionist);
        var patientClient = _accounts.As(patient);

        var questionnaireId = await NewQuestionnaireFromTemplateAsync(nutritionistClient);
        var list = await nutritionistClient.GetFromJsonAsync<JsonElement>("/api/v1/nutritionists/me/questionnaires", TestAccounts.Json, _ct);
        Assert.True(list.GetProperty("items")[0].GetProperty("questionCount").GetInt32() > 1);

        var anamnesisId = await SendAsync(nutritionistClient, patient.Id, questionnaireId);

        var pending = await patientClient.GetFromJsonAsync<JsonElement>("/api/v1/patients/me/anamneses", TestAccounts.Json, _ct);
        Assert.Equal("Pending", pending.GetProperty("items")[0].GetProperty("status").GetString());

        var detail = await patientClient.GetFromJsonAsync<JsonElement>($"/api/v1/patients/me/anamneses/{anamnesisId}", TestAccounts.Json, _ct);
        var goal = detail.GetProperty("questions")[0];
        Assert.Equal("SingleChoice", goal.GetProperty("type").GetString());
        Assert.Equal(JsonValueKind.Null, detail.GetProperty("answers").ValueKind);

        // A pergunta obrigatória em branco: 422, e a anamnese continua pendente.
        var blank = await patientClient.PostAsJsonAsync($"/api/v1/patients/me/anamneses/{anamnesisId}/answers", new { answers = Array.Empty<object>() }, TestAccounts.Json, _ct);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, blank.StatusCode);

        var answers = new { answers = new[] { new { questionId = goal.GetProperty("id").GetGuid(), options = new[] { "Emagrecer" } } } };
        var submitted = await patientClient.PostAsJsonAsync($"/api/v1/patients/me/anamneses/{anamnesisId}/answers", answers, TestAccounts.Json, _ct);
        Assert.Equal(HttpStatusCode.NoContent, submitted.StatusCode);

        // D-V2-29: travada depois de enviada.
        var again = await patientClient.PostAsJsonAsync($"/api/v1/patients/me/anamneses/{anamnesisId}/answers", answers, TestAccounts.Json, _ct);
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);

        // D-V2-31: editar o questionário depois não muda a anamnese já enviada.
        var edited = await nutritionistClient.PutAsJsonAsync($"/api/v1/nutritionists/me/questionnaires/{questionnaireId}", new
        {
            title = "Retorno",
            questions = new[] { new { text = "Como foi a semana?", type = "Text", required = true } }
        }, TestAccounts.Json, _ct);
        Assert.Equal(HttpStatusCode.NoContent, edited.StatusCode);

        var read = await nutritionistClient.GetFromJsonAsync<JsonElement>($"/api/v1/nutritionists/me/anamneses/{anamnesisId}", TestAccounts.Json, _ct);
        Assert.Equal("Submitted", read.GetProperty("status").GetString());
        Assert.Equal("Anamnese inicial", read.GetProperty("title").GetString());
        Assert.Equal(goal.GetProperty("text").GetString(), read.GetProperty("questions")[0].GetProperty("text").GetString());
        var answer = Assert.Single(read.GetProperty("answers").EnumerateArray());
        Assert.Equal("Emagrecer", answer.GetProperty("options")[0].GetString());

        var patientList = await nutritionistClient.GetFromJsonAsync<JsonElement>(
            $"/api/v1/nutritionists/me/patients/{patient.Id}/anamneses", TestAccounts.Json, _ct);
        Assert.Equal(1, patientList.GetProperty("totalItems").GetInt32());
    }

    // D-V2-06 e D-V2-30: só o nutricionista que acompanha. Sem o vínculo, ele perde o acesso, e o paciente continua lendo.
    [Fact]
    public async Task Only_The_Following_Nutritionist_Should_Reach_The_Anamnesis()
    {
        var patient = await _accounts.NewPatientAsync();
        var nutritionist = await _accounts.NewNutritionistAsync();
        await _accounts.FollowAsync(patient, nutritionist);
        var nutritionistClient = _accounts.As(nutritionist);
        var stranger = _accounts.As(await _accounts.NewNutritionistAsync());

        var questionnaireId = await NewQuestionnaireFromTemplateAsync(nutritionistClient);
        var anamnesisId = await SendAsync(nutritionistClient, patient.Id, questionnaireId);

        Assert.Equal(HttpStatusCode.NotFound, (await stranger.GetAsync($"/api/v1/nutritionists/me/questionnaires/{questionnaireId}", _ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await stranger.GetAsync($"/api/v1/nutritionists/me/anamneses/{anamnesisId}", _ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await stranger.GetAsync($"/api/v1/nutritionists/me/patients/{patient.Id}/anamneses", _ct)).StatusCode);
        var strangerQuestionnaire = await NewQuestionnaireFromTemplateAsync(stranger);
        var strangerSend = await stranger.PostAsJsonAsync(
            $"/api/v1/nutritionists/me/patients/{patient.Id}/anamneses", new { questionnaireId = strangerQuestionnaire }, TestAccounts.Json, _ct);
        Assert.Equal(HttpStatusCode.NotFound, strangerSend.StatusCode);

        var otherPatient = _accounts.As(await _accounts.NewPatientAsync());
        Assert.Equal(HttpStatusCode.NotFound, (await otherPatient.GetAsync($"/api/v1/patients/me/anamneses/{anamnesisId}", _ct)).StatusCode);

        var end = await _accounts.As(patient).PostAsync("/api/v1/patients/me/care-relationship/end", null, _ct);
        Assert.Equal(HttpStatusCode.NoContent, end.StatusCode);

        Assert.Equal(HttpStatusCode.NotFound, (await nutritionistClient.GetAsync($"/api/v1/nutritionists/me/anamneses/{anamnesisId}", _ct)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _accounts.As(patient).GetAsync($"/api/v1/patients/me/anamneses/{anamnesisId}", _ct)).StatusCode);

        // A pendente não pode mais ser respondida: ninguém leria as respostas.
        var late = await _accounts.As(patient).PostAsJsonAsync(
            $"/api/v1/patients/me/anamneses/{anamnesisId}/answers", new { answers = Array.Empty<object>() }, TestAccounts.Json, _ct);
        Assert.Equal(HttpStatusCode.Conflict, late.StatusCode);
    }
}
