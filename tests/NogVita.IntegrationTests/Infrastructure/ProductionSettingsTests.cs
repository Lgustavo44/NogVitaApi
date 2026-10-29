namespace NogVita.IntegrationTests.Infrastructure;

// Fase 26, Etapa 3: em produção, a API se recusa a subir com uma configuração insegura ou incompleta.
public class ProductionSettingsTests(NogVitaApiFixture fixture)
{
    [Fact]
    public async Task Production_Should_Refuse_To_Start_With_The_Defaults()
    {
        // Os padrões que servem para desenvolver: AllowedHosts "*", sem CORS, o front em http e o e-mail local.
        await using var factory = fixture.CreateFactory("Production")
            .WithSettings(("Email:Provider", "Smtp"));

        var exception = Assert.ThrowsAny<Exception>(() => factory.Server);

        var message = exception.GetBaseException().Message;
        Assert.Contains("Configuração de produção incompleta", message);
        Assert.Contains("AllowedHosts", message);
        Assert.Contains("Cors:AllowedOrigins", message);
        Assert.Contains("Frontend:BaseUrl", message);
        Assert.Contains("Email:Provider", message);
    }

    [Fact]
    public async Task Production_Should_Start_When_Everything_Is_Configured()
    {
        await using var factory = fixture.CreateFactory("Production").WithSettings(
            ("AllowedHosts", "api.nogvita.com.br"),
            ("Cors:AllowedOrigins:0", "https://nogvita.com.br"),
            ("Frontend:BaseUrl", "https://nogvita.com.br"));

        var client = factory.CreateClient(new() { BaseAddress = new Uri("https://api.nogvita.com.br") });
        var response = await client.GetAsync("/health/live", TestContext.Current.CancellationToken);

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    // Em produção, nem a tela do Swagger nem o documento OpenAPI ficam expostos (só no Development e no Staging).
    [Theory]
    [InlineData("/swagger/index.html")]
    [InlineData("/openapi/v1.json")]
    public async Task Production_Should_Not_Expose_The_Swagger(string path)
    {
        await using var factory = fixture.CreateFactory("Production").WithSettings(
            ("AllowedHosts", "api.nogvita.com.br"),
            ("Cors:AllowedOrigins:0", "https://nogvita.com.br"),
            ("Frontend:BaseUrl", "https://nogvita.com.br"));

        var client = factory.CreateClient(new() { BaseAddress = new Uri("https://api.nogvita.com.br") });
        var response = await client.GetAsync(path, TestContext.Current.CancellationToken);

        // Sem a rota, quem responde é a política de fallback (D162), que vale também para endereços inexistentes:
        // um anônimo recebe 401, e não descobre nem o que existe. O que importa é que o Swagger nunca responde 200.
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
