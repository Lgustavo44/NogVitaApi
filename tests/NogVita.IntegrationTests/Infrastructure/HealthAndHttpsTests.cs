using System.Net;

namespace NogVita.IntegrationTests.Infrastructure;

// Fase 26, Etapa 1: o /health separado em "viva" e "pronta", e o HTTPS como ele funciona atrás da Render.
public class HealthAndHttpsTests(NogVitaApiFixture fixture)
{
    private const string StagingHost = "nogvitaapi.onrender.com";
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    [Theory]
    [InlineData("/health/live")]
    [InlineData("/health/ready")]
    [InlineData("/health")]
    public async Task Health_Endpoints_Should_Answer_Healthy(string path)
    {
        var response = await fixture.CreateClient().GetAsync(path, _ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync(_ct));
    }

    // Uma API "como no staging": ambiente Staging, e as requisições chegando pelo endereço público.
    private async Task<(NogVitaApiFactory Factory, HttpClient Client)> StagingAsync()
    {
        var factory = fixture.CreateFactory("Staging");
        var client = factory.CreateClient(new() { BaseAddress = new Uri($"http://{StagingHost}"), AllowAutoRedirect = false });
        return (factory, client);
    }

    [Fact]
    public async Task Plain_Http_Should_Be_Redirected_To_Https_Keeping_The_Method()
    {
        var (factory, client) = await StagingAsync();
        await using var _ = factory;

        var response = await client.PostAsync("/api/v1/auth/login", null, _ct);

        Assert.Equal(HttpStatusCode.PermanentRedirect, response.StatusCode); // 308: o POST continua POST
        Assert.Equal($"https://{StagingHost}/api/v1/auth/login", response.Headers.Location?.ToString());
    }

    // A Render termina o TLS e avisa a API pelo X-Forwarded-Proto: ela não redireciona, e manda o HSTS.
    [Fact]
    public async Task Https_Through_The_Proxy_Should_Not_Redirect_And_Should_Send_Hsts()
    {
        var (factory, client) = await StagingAsync();
        await using var _ = factory;

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/foods?search=arroz");
        request.Headers.Add("X-Forwarded-Proto", "https");
        var response = await client.SendAsync(request, _ct);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode); // chegou ao pipeline, sem redirecionar
        Assert.Equal("max-age=2592000", response.Headers.GetValues("Strict-Transport-Security").Single()); // 30 dias
    }

    // A checagem de saúde da plataforma chega por HTTP, direto no container: um 308 faria a API parecer fora do ar.
    [Theory]
    [InlineData("/health")]
    [InlineData("/health/live")]
    [InlineData("/health/ready")]
    public async Task Health_Should_Answer_Over_Plain_Http(string path)
    {
        var (factory, client) = await StagingAsync();
        await using var _ = factory;

        var response = await client.GetAsync(path, _ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
