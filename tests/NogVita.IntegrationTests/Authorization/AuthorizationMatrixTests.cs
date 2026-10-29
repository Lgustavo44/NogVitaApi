using System.Net;
using System.Text;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using NogVita.IntegrationTests.Infrastructure;

namespace NogVita.IntegrationTests.Authorization;

// Etapa 3: quem pode chamar o quê. A matriz é escrita à mão, de propósito: ela é a especificação.
// Se uma rota nova aparecer sem entrar aqui, o primeiro teste falha (foi assim que, na Fase 21, uma rota podia ficar aberta).
public class AuthorizationMatrixTests(NogVitaApiFixture fixture)
{
    private const string Anyone = "Anônimo";
    private const string AnyUser = "Qualquer usuário";
    private const string Patient = "Patient";
    private const string Nutritionist = "Nutritionist";
    private const string Admin = "Admin";
    private const string NutritionistOrAdmin = "Nutritionist,Admin";

    private static readonly (string Method, string Route, string Allowed)[] Matrix =
    [
        ("GET", "health", Anyone),
        ("GET", "health/live", Anyone),
        ("GET", "health/ready", Anyone),
        ("GET", "openapi/{documentName}.json", Anyone), // o documento do OpenAPI (no ambiente de testes, sem a tela do Swagger)

        // Autenticação: aberta, menos o "quem sou eu"
        ("POST", "api/v1/auth/login", Anyone),
        ("GET", "api/v1/auth/me", AnyUser),
        ("POST", "api/v1/auth/refresh", Anyone),
        ("POST", "api/v1/auth/logout", Anyone),
        ("POST", "api/v1/auth/register/patient", Anyone),
        ("POST", "api/v1/auth/email-confirmation/confirm", Anyone),
        ("POST", "api/v1/auth/email-confirmation/resend", Anyone),
        ("POST", "api/v1/auth/password-reset/request", Anyone),
        ("POST", "api/v1/auth/password-reset/confirm", Anyone),
        ("POST", "api/v1/auth/invitations/accept", Anyone),

        // Paciente
        ("GET", "api/v1/patients/me", Patient),
        ("PUT", "api/v1/patients/me", Patient),
        ("POST", "api/v1/patients/me/nutritionist-requests", Patient),
        ("GET", "api/v1/patients/me/nutritionist-requests", Patient),
        ("POST", "api/v1/patients/me/nutritionist-requests/{id:guid}/cancel", Patient),
        ("GET", "api/v1/patients/me/care-relationship", Patient),
        ("POST", "api/v1/patients/me/care-relationship/end", Patient),
        ("GET", "api/v1/patients/me/nutrition-plans", Patient),
        ("GET", "api/v1/patients/me/nutrition-plans/current", Patient),
        ("GET", "api/v1/patients/me/nutrition-plans/{id:guid}", Patient),
        ("GET", "api/v1/patients/me/nutrition-plans/{id:guid}/pdf", Patient),
        ("POST", "api/v1/patients/me/nutrition-plans/{id:guid}/pdf/email", Patient),
        ("POST", "api/v1/patients/me/weight-records", Patient),
        ("GET", "api/v1/patients/me/weight-records", Patient),
        ("DELETE", "api/v1/patients/me/weight-records/{id:guid}", Patient),

        // Lista de nutricionistas: qualquer usuário conectado; o próprio perfil, só o nutricionista
        ("GET", "api/v1/nutritionists", AnyUser),
        ("GET", "api/v1/nutritionists/me", Nutritionist),
        ("PUT", "api/v1/nutritionists/me", Nutritionist),

        // Nutricionista: acompanhamento, planos, refeições e peso
        ("GET", "api/v1/nutritionists/me/requests", Nutritionist),
        ("POST", "api/v1/nutritionists/me/requests/{id:guid}/accept", Nutritionist),
        ("POST", "api/v1/nutritionists/me/requests/{id:guid}/reject", Nutritionist),
        ("GET", "api/v1/nutritionists/me/patients", Nutritionist),
        ("GET", "api/v1/nutritionists/me/patients/{patientId:guid}", Nutritionist),
        ("POST", "api/v1/nutritionists/me/patients/{patientId:guid}/end", Nutritionist),
        ("POST", "api/v1/nutritionists/me/nutrition-plans", Nutritionist),
        ("GET", "api/v1/nutritionists/me/patients/{patientId:guid}/nutrition-plans", Nutritionist),
        ("GET", "api/v1/nutritionists/me/nutrition-plans/{id:guid}", Nutritionist),
        ("GET", "api/v1/nutritionists/me/nutrition-plans/{id:guid}/pdf", Nutritionist),
        ("PUT", "api/v1/nutritionists/me/nutrition-plans/{id:guid}", Nutritionist),
        ("POST", "api/v1/nutritionists/me/nutrition-plans/{id:guid}/activate", Nutritionist),
        ("POST", "api/v1/nutritionists/me/nutrition-plans/{id:guid}/finish", Nutritionist),
        ("POST", "api/v1/nutritionists/me/nutrition-plans/{id:guid}/cancel", Nutritionist),
        ("PUT", "api/v1/nutritionists/me/nutrition-plans/{id:guid}/target", Nutritionist),
        ("DELETE", "api/v1/nutritionists/me/nutrition-plans/{id:guid}/target", Nutritionist),
        ("POST", "api/v1/nutritionists/me/nutrition-plans/{planId:guid}/meals", Nutritionist),
        ("PUT", "api/v1/nutritionists/me/nutrition-plans/{planId:guid}/meals/{mealId:guid}", Nutritionist),
        ("DELETE", "api/v1/nutritionists/me/nutrition-plans/{planId:guid}/meals/{mealId:guid}", Nutritionist),
        ("PUT", "api/v1/nutritionists/me/nutrition-plans/{planId:guid}/meals/order", Nutritionist),
        ("POST", "api/v1/nutritionists/me/nutrition-plans/{planId:guid}/meals/{mealId:guid}/foods", Nutritionist),
        ("PUT", "api/v1/nutritionists/me/nutrition-plans/{planId:guid}/meals/{mealId:guid}/foods/{mealFoodId:guid}", Nutritionist),
        ("DELETE", "api/v1/nutritionists/me/nutrition-plans/{planId:guid}/meals/{mealId:guid}/foods/{mealFoodId:guid}", Nutritionist),
        ("POST", "api/v1/nutritionists/me/patients/{patientId:guid}/weight-records", Nutritionist),
        ("GET", "api/v1/nutritionists/me/patients/{patientId:guid}/weight-records", Nutritionist),
        ("DELETE", "api/v1/nutritionists/me/weight-records/{id:guid}", Nutritionist),

        // Alimentos: quem monta planos e quem cuida da base
        ("GET", "api/v1/foods", NutritionistOrAdmin),
        ("GET", "api/v1/foods/{id:guid}", NutritionistOrAdmin),
        ("GET", "api/v1/foods/barcode/{barcode}", NutritionistOrAdmin),
        ("POST", "api/v1/foods/barcode/{barcode}/import", NutritionistOrAdmin),

        // Administração
        ("GET", "api/v1/admin/users", Admin),
        ("GET", "api/v1/admin/users/{id:guid}", Admin),
        ("POST", "api/v1/admin/users/{id:guid}/deactivate", Admin),
        ("POST", "api/v1/admin/users/{id:guid}/activate", Admin),
        ("GET", "api/v1/admin/patients", Admin),
        ("GET", "api/v1/admin/patients/{id:guid}", Admin),
        ("GET", "api/v1/admin/nutritionists", Admin),
        ("GET", "api/v1/admin/nutritionists/{id:guid}", Admin),
        ("POST", "api/v1/admin/nutritionists", Admin),
        ("POST", "api/v1/admin/nutritionists/{id:guid}/invitation", Admin),
        ("DELETE", "api/v1/admin/nutritionists/{id:guid}", Admin),
        ("GET", "api/v1/admin/foods", Admin),
        ("POST", "api/v1/admin/foods/{id:guid}/activate", Admin),
        ("POST", "api/v1/admin/foods/{id:guid}/deactivate", Admin),
    ];

    private readonly TestAccounts _accounts = new(fixture);

    [Fact]
    public void Every_Route_Of_The_Api_Should_Be_In_The_Matrix()
    {
        var actual = fixture.Factory.Services.GetRequiredService<EndpointDataSource>().Endpoints
            .OfType<RouteEndpoint>()
            .SelectMany(e => (e.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods ?? ["GET"])
                .Select(method => $"{method} {e.RoutePattern.RawText!.Trim('/')}"))
            .ToHashSet();
        var expected = Matrix.Select(r => $"{r.Method} {r.Route}").ToHashSet();

        Assert.Empty(actual.Except(expected)); // rota nova, sem decisão de quem pode chamar
        Assert.Empty(expected.Except(actual)); // rota da matriz que não existe mais
    }

    [Fact]
    public async Task Each_Role_Should_Reach_Only_Its_Own_Routes()
    {
        var callers = new (string Role, HttpClient Client)[]
        {
            (Anyone, _accounts.Anonymous()),
            (Patient, _accounts.As(await _accounts.NewPatientAsync())),
            (Nutritionist, _accounts.As(await _accounts.NewNutritionistAsync())),
            (Admin, _accounts.As(await _accounts.AdminAsync())),
        };

        var failures = new List<string>();

        foreach (var (method, route, allowed) in Matrix)
        {
            foreach (var (role, client) in callers)
            {
                var status = await SendAsync(client, method, route);
                var mayCall = allowed == Anyone
                    || (allowed == AnyUser && role != Anyone)
                    || allowed.Split(',').Contains(role);

                // Quem pode passa pela autorização (a resposta pode ser 400, 404...); quem não pode recebe 401 ou 403.
                var expected = mayCall ? "passar" : role == Anyone ? "401" : "403";
                var ok = mayCall
                    ? status is not (HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                    : status == (role == Anyone ? HttpStatusCode.Unauthorized : HttpStatusCode.Forbidden);

                if (!ok)
                {
                    failures.Add($"{method} {route} como {role}: esperado {expected}, veio {(int)status}");
                }
            }
        }

        Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
    }

    // Os parâmetros da rota viram valores que passam pelas restrições ({id:guid}) mas não existem no banco.
    private static async Task<HttpStatusCode> SendAsync(HttpClient client, string method, string route)
    {
        var url = "/" + System.Text.RegularExpressions.Regex.Replace(route, @"\{(\w+)(:guid)?\}",
            m => m.Groups[2].Success ? Guid.NewGuid().ToString() : "codigo-invalido");

        using var request = new HttpRequestMessage(new HttpMethod(method), url);
        if (method is "POST" or "PUT")
        {
            request.Content = new StringContent("{}", Encoding.UTF8, "application/json");
        }

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);
        return response.StatusCode;
    }
}
