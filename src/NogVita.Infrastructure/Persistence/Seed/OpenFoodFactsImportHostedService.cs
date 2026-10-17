using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace NogVita.Infrastructure.Persistence.Seed;

// D154: a importação em massa roda em segundo plano, depois que a API já está atendendo.
internal sealed class OpenFoodFactsImportHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<OpenFoodFactsImportHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            // O BackgroundService é singleton, e o DbContext é scoped: o escopo vive só durante a importação.
            using var scope = scopeFactory.CreateScope();
            var seeder = scope.ServiceProvider.GetRequiredService<OpenFoodFactsBulkSeeder>();

            await seeder.SeedAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("Open Food Facts: importação interrompida pelo desligamento da API. Ela continua na próxima inicialização.");
        }
        catch (Exception exception)
        {
            // Desde o .NET 6, uma exceção não tratada num BackgroundService derruba a API inteira.
            logger.LogError(exception, "Open Food Facts: a importação em massa falhou.");
        }
    }
}
