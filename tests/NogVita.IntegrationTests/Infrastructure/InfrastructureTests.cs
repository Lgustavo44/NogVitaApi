using System.Net;
using Microsoft.EntityFrameworkCore;

namespace NogVita.IntegrationTests.Infrastructure;

// Os primeiros testes: conferem que a própria infraestrutura funciona (o container, as migrations,
// a inicialização da API e o pipeline HTTP), antes de qualquer fluxo de negócio.
public class InfrastructureTests(NogVitaApiFixture fixture)
{
    [Fact]
    public async Task Health_Should_Reach_The_Real_Database()
    {
        var response = await fixture.CreateClient().GetAsync("/health", TestContext.Current.CancellationToken);

        // O /health inclui a checagem do DbContext: 200 só se o Postgres do container respondeu.
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Startup_Should_Seed_Taco_But_Not_Import_Open_Food_Facts()
    {
        await using var context = fixture.CreateDbContext();

        // Os da TACO oficial: o teste do alimento inativo cria um "Alimento de teste ..." com a fonte TACO, e ele não conta.
        var taco = await context.Foods.CountAsync(
            f => f.Source == Domain.Foods.FoodSource.Taco && !f.Name.StartsWith("Alimento de teste"), TestContext.Current.CancellationToken);
        var openFoodFacts = await context.Foods.CountAsync(f => f.Source == Domain.Foods.FoodSource.OpenFoodFacts, TestContext.Current.CancellationToken);

        Assert.Equal(597, taco);
        // D181: a importação em massa (22.728 produtos) fica desligada nos testes. Um ou outro produto avulso,
        // criado por um teste (como o da relevância da busca), não conta.
        Assert.True(openFoodFacts < 100, $"Produtos do Open Food Facts: {openFoodFacts}");
    }

    [Fact]
    public async Task Protected_Route_Without_Token_Should_Be_401_With_Security_Headers()
    {
        var response = await fixture.CreateClient().GetAsync("/api/v1/foods?search=arroz", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").Single());
        Assert.Equal("no-referrer", response.Headers.GetValues("Referrer-Policy").Single());
        Assert.Contains("default-src 'none'", response.Headers.GetValues("Content-Security-Policy").Single());
        Assert.True(response.Headers.CacheControl?.NoStore);
        Assert.False(response.Headers.Contains("Server"));
    }

    [Fact]
    public async Task Validation_Error_Should_Be_A_Problem_Details()
    {
        var response = await fixture.CreateClient().PostAsync(
            "/api/v1/auth/login",
            new StringContent("""{ "email": "nao-e-um-email", "password": "" }""", System.Text.Encoding.UTF8, "application/json"),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }
}
