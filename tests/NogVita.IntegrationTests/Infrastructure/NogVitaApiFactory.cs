using System.Security.Cryptography;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NogVita.Application.Abstractions;

namespace NogVita.IntegrationTests.Infrastructure;

// A API inteira, dentro do processo do teste: o mesmo Program.cs da produção, com o banco do container.
public sealed class NogVitaApiFactory(string connectionString, CapturedEmailSender emailSender, string environment = "Testing")
    : WebApplicationFactory<Program>
{
    // O admin inicial, criado pelo AdminSeeder na inicialização, como em produção.
    public const string AdminEmail = "admin@nogvita.test";
    public const string AdminCpf = "52998224725";
    public static readonly string AdminPassword = $"admin-dos-testes-{Guid.NewGuid():N}";

    // A mesma API, com algumas configurações trocadas (aplicadas depois das da fábrica, então elas vencem).
    public WebApplicationFactory<Program> WithSettings(params (string Key, string Value)[] settings) =>
        WithWebHostBuilder(builder =>
        {
            foreach (var (key, value) in settings)
            {
                builder.UseSetting(key, value);
            }
        });

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Um ambiente próprio: sem os User Secrets do Development, e sem o Swagger.
        builder.UseEnvironment(environment);

        // No staging, o e-mail vai pela API da Brevo, que exige uma chave. O envio é trocado pelo dublê de qualquer jeito.
        builder.UseSetting("Email:ApiKey", "chave-falsa-dos-testes");

        // O UseSetting entra antes de o Program.cs ler a configuração (as seções são lidas no começo dele).
        builder.UseSetting("ConnectionStrings:NogVita", connectionString);
        builder.UseSetting("Jwt:SecretKey", Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));
        builder.UseSetting("Email:Host", "localhost");
        builder.UseSetting("Email:Port", "1025");
        builder.UseSetting("Email:FromAddress", "nao-responda@nogvita.test");
        builder.UseSetting("Email:FromName", "NogVita");
        builder.UseSetting("Frontend:BaseUrl", "http://localhost:5173");
        builder.UseSetting("AdminSeed:Name", "Admin dos Testes");
        builder.UseSetting("AdminSeed:Email", AdminEmail);
        builder.UseSetting("AdminSeed:Cpf", AdminCpf);
        builder.UseSetting("AdminSeed:Password", AdminPassword);

        // D181: o rate limit barraria a suíte (todas as requisições vêm do mesmo "IP"),
        // e a importação dos 22 mil produtos deixaria cada execução lenta.
        builder.UseSetting("RateLimiting:Enabled", "false");
        builder.UseSetting("OpenFoodFacts:ImportOnStartup", "false");

        // ConfigureTestServices, e não ConfigureServices: roda depois da configuração da aplicação,
        // então a troca vence o registro do Program (a armadilha do .NET 10 com o xUnit v3).
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IEmailSender>();
            services.AddSingleton<IEmailSender>(emailSender);
        });
    }
}
