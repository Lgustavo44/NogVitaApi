using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NogVita.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

[assembly: AssemblyFixture(typeof(NogVita.IntegrationTests.Infrastructure.NogVitaApiFixture))]

namespace NogVita.IntegrationTests.Infrastructure;

// D178: um Postgres por execução, e não por teste. O container sobe uma vez, as migrations rodam uma vez,
// e todos os testes do projeto usam o mesmo banco e a mesma API (por isso, D179: cada teste cria os próprios dados).
public sealed class NogVitaApiFixture : IAsyncLifetime
{
    // A mesma versão do container de desenvolvimento.
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17")
        .WithDatabase("nogvita")
        .WithUsername("nogvita")
        .WithPassword("nogvita-testes")
        .Build();

    private NogVitaApiFactory? _factory;

    public CapturedEmailSender Emails { get; } = new();

    public NogVitaApiFactory Factory => _factory ?? throw new InvalidOperationException("O fixture ainda não foi iniciado.");

    public async ValueTask InitializeAsync()
    {
        await _postgres.StartAsync();

        // As migrations rodam antes de a API subir: a inicialização dela já semeia a TACO e o admin,
        // e precisa das tabelas prontas.
        await using (var context = CreateDbContext())
        {
            await context.Database.MigrateAsync();
        }

        _factory = new NogVitaApiFactory(_postgres.GetConnectionString(), Emails);

        // Sobe a API uma vez aqui, para o primeiro teste não pagar o custo (e para uma falha de inicialização aparecer cedo).
        _ = _factory.Server;
    }

    public HttpClient CreateClient() => Factory.CreateClient();

    // Um DbContext direto no banco do container, para os testes conferirem o que foi gravado.
    public NogVitaDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<NogVitaDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .UseSnakeCaseNamingConvention()
            .Options);

    public async ValueTask DisposeAsync()
    {
        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }

        await _postgres.DisposeAsync();
    }
}
