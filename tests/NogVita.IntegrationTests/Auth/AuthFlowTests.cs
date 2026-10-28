using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using NogVita.IntegrationTests.Infrastructure;

namespace NogVita.IntegrationTests.Auth;

// Etapa 2: os fluxos de autenticação de ponta a ponta, pelo HTTP real e com o Postgres real.
public class AuthFlowTests(NogVitaApiFixture fixture)
{
    private readonly TestAccounts _accounts = new(fixture);
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task Patient_Should_Register_Confirm_By_Email_And_Log_In()
    {
        var patient = await _accounts.NewPatientAsync();

        var me = await _accounts.As(patient).GetFromJsonAsync<JsonElement>("/api/v1/auth/me", TestAccounts.Json, _ct);

        Assert.Equal(patient.Id.ToString(), me.GetProperty("userId").GetString());
        Assert.Equal(["Patient"], me.GetProperty("roles").EnumerateArray().Select(r => r.GetString()));
    }

    [Fact]
    public async Task Login_Before_Confirming_The_Email_Should_Be_403()
    {
        var email = TestAccounts.UniqueEmail("sem-confirmar");
        await _accounts.Anonymous().PostAsJsonAsync("/api/v1/auth/register/patient", TestAccounts.PatientRegistration(email), TestAccounts.Json, _ct);

        var login = await _accounts.Anonymous().PostAsJsonAsync(
            "/api/v1/auth/login", new { email, password = TestAccounts.Password }, TestAccounts.Json, _ct);

        Assert.Equal(HttpStatusCode.Forbidden, login.StatusCode);
    }

    // D130: o cadastro nunca revela se o e-mail já tem conta. A resposta é a mesma; quem tem conta recebe um aviso.
    [Fact]
    public async Task Registering_An_Existing_Email_Should_Look_The_Same_And_Warn_By_Email()
    {
        var patient = await _accounts.NewPatientAsync();

        var again = await _accounts.Anonymous().PostAsJsonAsync(
            "/api/v1/auth/register/patient", TestAccounts.PatientRegistration(patient.Email), TestAccounts.Json, _ct);

        Assert.Equal(HttpStatusCode.Accepted, again.StatusCode);
        Assert.Contains(fixture.Emails.SentTo(patient.Email), m => m.Subject == "Você já tem uma conta no NogVita");
    }

    [Fact]
    public async Task Wrong_Password_And_Unknown_Email_Should_Answer_The_Same()
    {
        var patient = await _accounts.NewPatientAsync();

        var wrongPassword = await _accounts.Anonymous().PostAsJsonAsync(
            "/api/v1/auth/login", new { email = patient.Email, password = "senha errada mas bem longa" }, TestAccounts.Json, _ct);
        var unknownEmail = await _accounts.Anonymous().PostAsJsonAsync(
            "/api/v1/auth/login", new { email = TestAccounts.UniqueEmail("ninguem"), password = "senha errada mas bem longa" }, TestAccounts.Json, _ct);

        Assert.Equal(HttpStatusCode.Unauthorized, wrongPassword.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, unknownEmail.StatusCode);
        Assert.Equal(await wrongPassword.Content.ReadAsStringAsync(_ct) is var a ? Title(a) : null,
                     await unknownEmail.Content.ReadAsStringAsync(_ct) is var b ? Title(b) : null);
    }

    [Fact]
    public async Task Refresh_Should_Rotate_And_Detect_Reuse()
    {
        var patient = await _accounts.NewPatientAsync();

        var first = await Refresh(patient.RefreshToken);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        var rotated = (await first.Content.ReadFromJsonAsync<AuthTokens>(TestAccounts.Json, _ct))!;
        Assert.NotEqual(patient.RefreshToken, rotated.RefreshToken);

        // Reusar o token antigo é sinal de roubo: a API recusa, e derruba também o par novo.
        Assert.Equal(HttpStatusCode.Unauthorized, (await Refresh(patient.RefreshToken)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Refresh(rotated.RefreshToken)).StatusCode);
    }

    [Fact]
    public async Task Logout_Should_Revoke_The_Refresh_Token()
    {
        var patient = await _accounts.NewPatientAsync();

        var logout = await _accounts.Anonymous().PostAsJsonAsync(
            "/api/v1/auth/logout", new { refreshToken = patient.RefreshToken }, TestAccounts.Json, _ct);

        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Refresh(patient.RefreshToken)).StatusCode);
    }

    [Fact]
    public async Task Password_Reset_Should_Change_The_Password_And_End_Sessions()
    {
        var patient = await _accounts.NewPatientAsync();
        const string newPassword = "a senha nova depois do esquecimento";

        var request = await _accounts.Anonymous().PostAsJsonAsync(
            "/api/v1/auth/password-reset/request", new { email = patient.Email }, TestAccounts.Json, _ct);
        Assert.Equal(HttpStatusCode.Accepted, request.StatusCode);

        var token = _accounts.LinkTokenSentTo(patient.Email, "/redefinir-senha");
        var confirm = await _accounts.Anonymous().PostAsJsonAsync(
            "/api/v1/auth/password-reset/confirm", new { token, newPassword }, TestAccounts.Json, _ct);
        Assert.Equal(HttpStatusCode.NoContent, confirm.StatusCode);

        Assert.Equal(HttpStatusCode.Unauthorized, (await Login(patient.Email, patient.Password)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Login(patient.Email, newPassword)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Refresh(patient.RefreshToken)).StatusCode);
        Assert.Contains(fixture.Emails.SentTo(patient.Email), m => m.Subject == "Sua senha do NogVita foi alterada");

        // O link é de uso único.
        var reuse = await _accounts.Anonymous().PostAsJsonAsync(
            "/api/v1/auth/password-reset/confirm", new { token, newPassword = "mais uma senha nova e longa" }, TestAccounts.Json, _ct);
        Assert.Equal(HttpStatusCode.BadRequest, reuse.StatusCode);
    }

    [Fact]
    public async Task Nutritionist_Should_Enter_Through_The_Invitation()
    {
        var nutritionist = await _accounts.NewNutritionistAsync();

        var me = await _accounts.As(nutritionist).GetFromJsonAsync<JsonElement>("/api/v1/auth/me", TestAccounts.Json, _ct);

        Assert.Contains("Nutritionist", me.GetProperty("roles").EnumerateArray().Select(r => r.GetString()));
    }

    // D163: o bloqueio do admin vale na hora para o login e para a renovação do token.
    [Fact]
    public async Task Account_Blocked_By_Admin_Should_Not_Log_In_Nor_Refresh()
    {
        var patient = await _accounts.NewPatientAsync();
        var admin = await _accounts.AdminAsync();

        var deactivate = await _accounts.As(admin).PostAsync($"/api/v1/admin/users/{patient.Id}/deactivate", null, _ct);
        Assert.Equal(HttpStatusCode.NoContent, deactivate.StatusCode);

        Assert.Equal(HttpStatusCode.Unauthorized, (await Login(patient.Email, patient.Password)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Refresh(patient.RefreshToken)).StatusCode);
    }

    private Task<HttpResponseMessage> Login(string email, string password) =>
        _accounts.Anonymous().PostAsJsonAsync("/api/v1/auth/login", new { email, password }, TestAccounts.Json, _ct);

    private Task<HttpResponseMessage> Refresh(string refreshToken) =>
        _accounts.Anonymous().PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken }, TestAccounts.Json, _ct);

    private static string? Title(string problemJson) =>
        JsonDocument.Parse(problemJson).RootElement.TryGetProperty("title", out var title) ? title.GetString() : null;
}
