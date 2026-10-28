using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace NogVita.IntegrationTests.Infrastructure;

// Uma conta pronta para usar nos testes: o Id, o e-mail, a senha e o token de acesso.
public sealed record TestAccount(Guid Id, string Email, string Password, string AccessToken, string RefreshToken);

// D179: cada teste cria os próprios usuários, sempre com e-mail e CPF únicos, pelo mesmo caminho de um usuário real
// (cadastro → e-mail → confirmação → login). Nada é inserido direto no banco.
public sealed partial class TestAccounts(NogVitaApiFixture fixture)
{
    public const string Password = "uma senha bem longa para os testes";

    // A API devolve os enums como texto (JsonStringEnumConverter): os testes leem do mesmo jeito.
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private static int _nextCpf = 100_000_000;
    private static int _nextCrn = 100_000;

    public static string UniqueEmail(string prefix) => $"{prefix}.{Guid.NewGuid():N}@nogvita.test";

    // Um CPF válido e único por chamada (o mesmo cálculo do TestCpf dos testes unitários).
    public static string UniqueCpf()
    {
        var digits = Interlocked.Increment(ref _nextCpf).ToString("D9");
        digits += CheckDigit(digits);
        digits += CheckDigit(digits);
        return digits;

        static int CheckDigit(string value)
        {
            var weight = value.Length + 1;
            var rest = value.Sum(d => (d - '0') * weight--) % 11;
            return rest < 2 ? 0 : 11 - rest;
        }
    }

    public static string UniqueCrn() => Interlocked.Increment(ref _nextCrn).ToString();

    public HttpClient Anonymous() => fixture.CreateClient();

    public HttpClient As(TestAccount account) => As(account.AccessToken);

    public HttpClient As(string accessToken)
    {
        var client = fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }

    public static object PatientRegistration(string email, string? cpf = null, string password = Password) => new
    {
        name = "Paciente de Teste",
        email,
        cpf = cpf ?? UniqueCpf(),
        password,
        birthDate = "1995-03-10",
        biologicalSex = "Female",
        heightInCm = 165,
        goal = "WeightLoss"
    };

    // O link que chegou por e-mail, como a pessoa o veria (o token vai no fragmento da URL).
    public string LinkTokenSentTo(string email, string path)
    {
        var message = fixture.Emails.SentTo(email).LastOrDefault(m => m.TextBody.Contains(path))
            ?? throw new InvalidOperationException($"Nenhum e-mail com {path} chegou para {email}.");

        return TokenInLink().Match(message.TextBody) is { Success: true } match
            ? match.Groups["token"].Value
            : throw new InvalidOperationException($"O e-mail para {email} não tem o link {path}.");
    }

    public async Task<AuthTokens> LoginAsync(string email, string password = Password)
    {
        var response = await Anonymous().PostAsJsonAsync("/api/v1/auth/login", new { email, password }, Json);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<AuthTokens>(Json))!;
    }

    public async Task<Guid> MyIdAsync(string accessToken)
    {
        var me = await As(accessToken).GetFromJsonAsync<JsonElement>("/api/v1/auth/me", Json);
        return Guid.Parse(me.GetProperty("userId").GetString()!);
    }

    // Cadastro → e-mail → confirmação → login: o caminho de um paciente de verdade.
    public async Task<TestAccount> NewPatientAsync()
    {
        var email = UniqueEmail("paciente");

        var register = await Anonymous().PostAsJsonAsync("/api/v1/auth/register/patient", PatientRegistration(email), Json);
        Assert.Equal(HttpStatusCode.Accepted, register.StatusCode);

        var token = LinkTokenSentTo(email, "/confirmar-email");
        var confirm = await Anonymous().PostAsJsonAsync("/api/v1/auth/email-confirmation/confirm", new { token }, Json);
        Assert.Equal(HttpStatusCode.NoContent, confirm.StatusCode);

        return await AccountAsync(email, Password);
    }

    public Task<TestAccount> AdminAsync() => AccountAsync(NogVitaApiFactory.AdminEmail, NogVitaApiFactory.AdminPassword);

    // Pré-cadastro pelo admin → convite por e-mail → aceite com a senha → login.
    public async Task<TestAccount> NewNutritionistAsync()
    {
        var admin = await AdminAsync();
        var email = UniqueEmail("nutri");

        var preRegister = await As(admin).PostAsJsonAsync("/api/v1/admin/nutritionists", new
        {
            name = "Nutri de Teste",
            email,
            cpf = UniqueCpf(),
            crnRegion = 3,
            crnNumber = UniqueCrn()
        }, Json);
        Assert.Equal(HttpStatusCode.Created, preRegister.StatusCode);

        var token = LinkTokenSentTo(email, "/nutri/convite");
        var accept = await Anonymous().PostAsJsonAsync("/api/v1/auth/invitations/accept", new { token, password = Password }, Json);
        Assert.Equal(HttpStatusCode.NoContent, accept.StatusCode);

        return await AccountAsync(email, Password);
    }

    // O paciente pede, o nutricionista aceita: o vínculo nasce como na vida real.
    public async Task FollowAsync(TestAccount patient, TestAccount nutritionist)
    {
        var request = await As(patient).PostAsJsonAsync(
            "/api/v1/patients/me/nutritionist-requests", new { nutritionistId = nutritionist.Id }, Json);
        Assert.Equal(HttpStatusCode.Created, request.StatusCode);
        var requestId = (await request.Content.ReadFromJsonAsync<JsonElement>(Json)).GetProperty("id").GetGuid();

        var accept = await As(nutritionist).PostAsync($"/api/v1/nutritionists/me/requests/{requestId}/accept", null);
        Assert.Equal(HttpStatusCode.NoContent, accept.StatusCode);
    }

    // Um plano do nutricionista para o paciente que ele acompanha, opcionalmente já ativo.
    public async Task<Guid> NewPlanAsync(TestAccount nutritionist, Guid patientId, bool activate = false)
    {
        var client = As(nutritionist);
        var create = await client.PostAsJsonAsync("/api/v1/nutritionists/me/nutrition-plans", new
        {
            patientId,
            name = "Plano de teste",
            startDate = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd")
        }, Json);
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var planId = (await create.Content.ReadFromJsonAsync<JsonElement>(Json)).GetProperty("id").GetGuid();

        if (activate)
        {
            var activated = await client.PostAsync($"/api/v1/nutritionists/me/nutrition-plans/{planId}/activate", null);
            Assert.Equal(HttpStatusCode.NoContent, activated.StatusCode);
        }

        return planId;
    }

    public async Task<Guid> NewMealAsync(TestAccount nutritionist, Guid planId, string name)
    {
        var response = await As(nutritionist).PostAsJsonAsync(
            $"/api/v1/nutritionists/me/nutrition-plans/{planId}/meals", new { name }, Json);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<JsonElement>(Json)).GetProperty("id").GetGuid();
    }

    public async Task AddFoodAsync(TestAccount nutritionist, Guid planId, Guid mealId, Guid foodId, decimal quantityInGrams)
    {
        var response = await As(nutritionist).PostAsJsonAsync(
            $"/api/v1/nutritionists/me/nutrition-plans/{planId}/meals/{mealId}/foods", new { foodId, quantityInGrams }, Json);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private async Task<TestAccount> AccountAsync(string email, string password)
    {
        var tokens = await LoginAsync(email, password);
        var id = await MyIdAsync(tokens.AccessToken);
        return new TestAccount(id, email, password, tokens.AccessToken, tokens.RefreshToken);
    }

    [GeneratedRegex("#token=(?<token>[A-Za-z0-9_-]+)")]
    private static partial Regex TokenInLink();
}

public sealed record AuthTokens(string AccessToken, DateTime AccessTokenExpiresAtUtc, string RefreshToken, DateTime RefreshTokenExpiresAtUtc);
