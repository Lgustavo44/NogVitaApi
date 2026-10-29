using System.Net;
using System.Net.Http.Json;

namespace NogVita.IntegrationTests.Infrastructure;

// O front chama a API direto do navegador, de outra origem: sem o CORS certo, o navegador esconde todas as respostas.
// Usa a configuração do staging (appsettings.Staging.json), que libera só o front da Vercel.
public class CorsTests(NogVitaApiFixture fixture)
{
    private const string Front = "https://nogvita.vercel.app";
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    private (NogVitaApiFactory Factory, HttpClient Client) Staging()
    {
        var factory = fixture.CreateFactory("Staging");
        var client = factory.CreateClient(new() { BaseAddress = new Uri("https://nogvitaapi.onrender.com") });
        return (factory, client);
    }

    // O preflight chega sem token: se o CORS viesse depois da autenticação, ele levaria 401 e nada funcionaria.
    [Fact]
    public async Task Preflight_From_The_Front_Should_Be_Allowed_Without_A_Token()
    {
        var (factory, client) = Staging();
        await using var _ = factory;

        var request = new HttpRequestMessage(HttpMethod.Options, "/api/v1/patients/me");
        request.Headers.Add("Origin", Front);
        request.Headers.Add("Access-Control-Request-Method", "PUT");
        request.Headers.Add("Access-Control-Request-Headers", "authorization,content-type");

        var response = await client.SendAsync(request, _ct);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(Front, response.Headers.GetValues("Access-Control-Allow-Origin").Single());
        Assert.False(response.Headers.Contains("Access-Control-Allow-Credentials"));
    }

    // Sem o cabeçalho também nos erros, o front não lê o ProblemDetails e mostra uma falha de conexão.
    [Fact]
    public async Task Error_Responses_Should_Carry_The_Cors_Header()
    {
        var (factory, client) = Staging();
        await using var _ = factory;

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/login")
        {
            Content = JsonContent.Create(new { email = TestAccounts.UniqueEmail("cors"), password = "senha errada mas bem longa" })
        };
        request.Headers.Add("Origin", Front);

        var response = await client.SendAsync(request, _ct);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(Front, response.Headers.GetValues("Access-Control-Allow-Origin").Single());
    }

    [Fact]
    public async Task Other_Origins_Should_Not_Be_Allowed()
    {
        var (factory, client) = Staging();
        await using var _ = factory;

        var request = new HttpRequestMessage(HttpMethod.Options, "/api/v1/auth/login");
        request.Headers.Add("Origin", "https://outro-site.vercel.app");
        request.Headers.Add("Access-Control-Request-Method", "POST");

        var response = await client.SendAsync(request, _ct);

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }
}
