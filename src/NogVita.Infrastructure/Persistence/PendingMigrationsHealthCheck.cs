using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace NogVita.Infrastructure.Persistence;

// O banco precisa estar na versão do código. Sem esta checagem, uma versão nova da API subia com o banco
// desatualizado e respondia 500 em toda consulta que tocasse uma coluna nova (foi o login do staging, sem a
// coluna deactivated_at_utc da Fase 21). Com ela, o /health/ready falha, e a plataforma não manda tráfego
// para essa versão: o deploy falha à vista, em vez de virar erro para o usuário.
public sealed class PendingMigrationsHealthCheck(NogVitaDbContext context) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext healthContext, CancellationToken cancellationToken = default)
    {
        var pending = (await context.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();

        return pending.Count == 0
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy($"Migrations pendentes no banco: {string.Join(", ", pending)}.");
    }
}
