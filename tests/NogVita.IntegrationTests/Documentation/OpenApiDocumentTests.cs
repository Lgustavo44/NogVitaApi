using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NogVita.Api.Controllers;
using NogVita.IntegrationTests.Infrastructure;

namespace NogVita.IntegrationTests.Documentation;

// Fase 25: o Swagger precisa bastar para alguém que nunca viu o NogVita. Estes testes leem o documento gerado pela
// API real e conferem que nenhum endpoint ficou sem descrição (D183) ou sem as respostas que ele pode dar (D184).
// Um endpoint novo sem /// <summary> ou sem [ProducesResponseType] faz o teste falhar.
public class OpenApiDocumentTests(NogVitaApiFixture fixture)
{
    private static readonly string[] HttpMethods = ["get", "post", "put", "patch", "delete"];

    private async Task<JsonElement> DocumentAsync() =>
        await fixture.CreateClient().GetFromJsonAsync<JsonElement>("/openapi/v1.json", TestContext.Current.CancellationToken);

    private static IEnumerable<(string Name, JsonElement Operation)> Operations(JsonElement document) =>
        document.GetProperty("paths").EnumerateObject().SelectMany(path => path.Value.EnumerateObject()
            .Where(method => HttpMethods.Contains(method.Name))
            .Select(method => ($"{method.Name.ToUpperInvariant()} {path.Name}", method.Value)));

    private static HashSet<string> ResponseCodes(JsonElement operation) =>
        operation.GetProperty("responses").EnumerateObject().Select(r => r.Name).ToHashSet();

    [Fact]
    public async Task Document_Should_Have_Title_And_Description()
    {
        var info = (await DocumentAsync()).GetProperty("info");

        Assert.Equal("NogVita API", info.GetProperty("title").GetString());
        Assert.Equal("v1", info.GetProperty("version").GetString());
        var description = info.GetProperty("description").GetString();
        Assert.Contains("Autenticação", description);
        Assert.Contains("política de versionamento", description);
    }

    [Fact]
    public async Task Every_Endpoint_Should_Have_A_Summary()
    {
        var withoutSummary = Operations(await DocumentAsync())
            .Where(o => !o.Operation.TryGetProperty("summary", out var summary) || string.IsNullOrWhiteSpace(summary.GetString()))
            .Select(o => o.Name)
            .ToList();

        Assert.True(withoutSummary.Count == 0, "Sem /// <summary>: " + string.Join(", ", withoutSummary));
    }

    [Fact]
    public async Task Every_Endpoint_Should_Declare_Success_And_The_500()
    {
        var incomplete = Operations(await DocumentAsync())
            .Where(o => !o.Name.EndsWith("/health")) // o health check não é um controller
            .Select(o => (o.Name, Codes: ResponseCodes(o.Operation)))
            .Where(o => !o.Codes.Any(c => c.StartsWith('2')) || !o.Codes.Contains("500"))
            .Select(o => $"{o.Name} ({string.Join(", ", o.Codes)})")
            .ToList();

        Assert.True(incomplete.Count == 0, "Sem a resposta de sucesso ou o 500: " + string.Join("; ", incomplete));
    }

    [Fact]
    public async Task Protected_Endpoints_Should_Declare_The_401()
    {
        // As rotas abertas (login, cadastro, confirmação, senha e convite) ficam em /auth, menos o "quem sou eu".
        var anonymous = new[] { "/api/v1/auth/login", "/api/v1/auth/refresh", "/api/v1/auth/logout", "/api/v1/auth/register/",
            "/api/v1/auth/email-confirmation/", "/api/v1/auth/password-reset/", "/api/v1/auth/invitations/" };

        var missing = Operations(await DocumentAsync())
            .Where(o => o.Name.Contains("/api/") && !anonymous.Any(prefix => o.Name.Contains(prefix)))
            .Where(o => !ResponseCodes(o.Operation).Contains("401"))
            .Select(o => o.Name)
            .ToList();

        Assert.True(missing.Count == 0, "Rotas protegidas sem o 401: " + string.Join(", ", missing));
    }

    [Theory]
    [InlineData(typeof(EmailConfirmationController), nameof(EmailConfirmationController.Confirm), "POST /api/v1/auth/email-confirmation/confirm")]
    [InlineData(typeof(InvitationsController), nameof(InvitationsController.Accept), "POST /api/v1/auth/invitations/accept")]
    [InlineData(typeof(PasswordResetController), nameof(PasswordResetController.Confirm), "POST /api/v1/auth/password-reset/confirm")]
    [InlineData(typeof(PatientFollowUpsController), nameof(PatientFollowUpsController.RequestNutritionist), "POST /api/v1/patients/me/nutritionist-requests")]
    [InlineData(typeof(NutritionistMealsController), nameof(NutritionistMealsController.Reorder), "PUT /api/v1/nutritionists/me/nutrition-plans/{planId}/meals/order")]
    public async Task Business_Bad_Requests_Should_Declare_ProblemDetails(
        Type controllerType, string actionName, string operationName)
    {
        var action = controllerType.GetMethod(actionName);
        Assert.NotNull(action);

        var responseTypes = action.GetCustomAttributes<ProducesResponseTypeAttribute>();
        Assert.Contains(responseTypes, response =>
            response.StatusCode == StatusCodes.Status400BadRequest && response.Type == typeof(HttpValidationProblemDetails));
        Assert.Contains(responseTypes, response =>
            response.StatusCode == StatusCodes.Status400BadRequest && response.Type == typeof(ProblemDetails));

        var operation = Operations(await DocumentAsync()).Single(item => item.Name == operationName).Operation;
        var response = operation.GetProperty("responses").GetProperty("400");
        Assert.True(response.GetProperty("content").TryGetProperty("application/problem+json", out _));
    }

    // D185: o "quem sou eu" tipado aparece no documento, com os dois campos que o front já usa.
    [Fact]
    public async Task Me_Should_Be_Typed()
    {
        var document = await DocumentAsync();
        var me = document.GetProperty("components").GetProperty("schemas").GetProperty("MeResponse").GetProperty("properties");

        Assert.True(me.TryGetProperty("userId", out _));
        Assert.True(me.TryGetProperty("roles", out _));
    }

    // A API responde os enums como texto: o documento precisa dizer o mesmo.
    [Fact]
    public async Task Enums_Should_Be_Documented_As_Text()
    {
        var goal = (await DocumentAsync()).GetProperty("components").GetProperty("schemas").GetProperty("Goal");

        Assert.Contains("WeightLoss", goal.GetProperty("enum").EnumerateArray().Select(v => v.ToString()));
    }
}
