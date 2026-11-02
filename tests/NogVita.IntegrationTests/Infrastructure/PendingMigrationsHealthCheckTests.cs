using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NogVita.Infrastructure.Persistence;

namespace NogVita.IntegrationTests.Infrastructure;

// O /health/ready precisa falhar quando o banco está atrás do código. Foi o que derrubou o login do staging:
// o código novo subiu, o banco não tinha a coluna deactivated_at_utc, e toda consulta à tb_user virou 500.
public class PendingMigrationsHealthCheckTests(NogVitaApiFixture fixture)
{
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    private static Task<HealthCheckResult> CheckAsync(NogVitaDbContext context) =>
        new PendingMigrationsHealthCheck(context).CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);

    [Fact]
    public async Task Database_With_Every_Migration_Should_Be_Healthy()
    {
        await using var context = fixture.CreateDbContext();

        Assert.Equal(HealthStatus.Healthy, (await CheckAsync(context)).Status);
    }

    [Fact]
    public async Task Database_Behind_The_Code_Should_Be_Unhealthy()
    {
        // Um segundo banco, vazio, no mesmo container: o mesmo código, com o banco sem nenhuma migration.
        var emptyDatabase = $"vazio_{Guid.NewGuid():N}";
        await using (var context = fixture.CreateDbContext())
        {
            // O CREATE DATABASE não aceita parâmetro no nome; o nome é um GUID gerado aqui, sem entrada de fora.
#pragma warning disable EF1002
            await context.Database.ExecuteSqlRawAsync($"CREATE DATABASE {emptyDatabase}", _ct);
#pragma warning restore EF1002
        }

        await using var behind = fixture.CreateDbContext(emptyDatabase);
        var result = await CheckAsync(behind);

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Contains("AddAdminBlockAndAuditTrail", result.Description);
    }
}
