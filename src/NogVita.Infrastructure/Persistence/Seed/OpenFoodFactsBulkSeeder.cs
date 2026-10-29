using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NogVita.Domain.Common;
using NogVita.Domain.Foods;

namespace NogVita.Infrastructure.Persistence.Seed;

public sealed class OpenFoodFactsBulkSeeder(NogVitaDbContext context, ILogger<OpenFoodFactsBulkSeeder> logger)
{
    private const int BatchSize = 1000;

    // D192: o nome da marca desta importação na tb_seed_run.
    public const string SeedName = "open-food-facts-brasil";

    // O hash do CSV embutido nesta versão da API (o mesmo que a marca guarda).
    public static string CurrentFileHash() => OpenFoodFactsCsvReader.ComputeHash();

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        // D192: se este mesmo arquivo já foi importado até o fim, não há nada a fazer. Sem a marca, a cada vez que a API
        // acordava, ela buscava os 22.728 códigos e relia o CSV inteiro só para descobrir isso: 5,2 s de CPU no plano
        // gratuito da Render (0,1 CPU), logo depois de começar a atender.
        var contentHash = CurrentFileHash();
        var run = await context.SeedRuns.FindAsync([SeedName], cancellationToken);

        if (run?.ContentHash == contentHash)
        {
            logger.LogInformation("Open Food Facts: este arquivo já foi importado. Nada a fazer.");
            return;
        }

        // D152: só entram os códigos que ainda não existem (inclusive os importados um a um, e os desativados).
        var existingCodes = (await context.Foods
                .AsNoTracking()
                .Where(f => f.Source == FoodSource.OpenFoodFacts)
                .Select(f => f.SourceReference)
                .ToListAsync(cancellationToken))
            .ToHashSet();

        var batch = new List<Food>(BatchSize);
        var imported = 0;
        var rejected = 0;

        foreach (var row in OpenFoodFactsCsvReader.ReadRows())
        {
            // HashSet.Add devolve false para um código que já existe no banco ou que já apareceu no arquivo.
            if (!existingCodes.Add(row.Code))
            {
                continue;
            }

            try
            {
                var nutrients = new NutrientsPer100g(
                    row.EnergyKcal, row.Protein, row.Carbohydrate, row.Fat, row.Fiber, row.SodiumMg);

                batch.Add(Food.FromOpenFoodFacts(row.Code, row.Name, row.Brand, nutrients));
            }
            catch (DomainException exception)
            {
                // D153: uma linha recusada não derruba as outras.
                rejected++;
                logger.LogWarning("Open Food Facts: o produto {Code} foi recusado pelo domínio: {Reason}", row.Code, exception.Message);
                continue;
            }

            if (batch.Count == BatchSize)
            {
                imported += await SaveBatchAsync(batch, imported, cancellationToken);
            }
        }

        // O último lote, que pode ter menos de mil produtos.
        if (batch.Count > 0)
        {
            imported += await SaveBatchAsync(batch, imported, cancellationToken);
        }

        // A marca só é gravada no fim: uma importação interrompida (um deploy, a Render dormindo) não deixa marca,
        // e a próxima subida continua de onde parou, pela idempotência do código de barras (D152).
        var completedAt = DateTime.UtcNow;
        if (run is null)
        {
            context.SeedRuns.Add(new SeedRun(SeedName, contentHash, completedAt));
        }
        else
        {
            context.SeedRuns.Attach(run);
            run.Complete(contentHash, completedAt);
        }

        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Open Food Facts: importação concluída. {Imported} importados, {Rejected} recusados pelo domínio.",
            imported,
            rejected);
    }

    // D151: salva o lote e manda o EF "esquecer" os objetos, para a memória e o SaveChanges não crescerem a cada lote.
    private async Task<int> SaveBatchAsync(List<Food> batch, int importedSoFar, CancellationToken cancellationToken)
    {
        context.Foods.AddRange(batch);
        await context.SaveChangesAsync(cancellationToken);
        context.ChangeTracker.Clear();

        var saved = batch.Count;
        batch.Clear();

        logger.LogInformation("Open Food Facts: {Imported} produtos importados até agora.", importedSoFar + saved);
        return saved;
    }
}
