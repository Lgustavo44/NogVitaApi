using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic.FileIO;
using NogVita.Domain.Common;
using NogVita.Domain.Foods;

namespace NogVita.Infrastructure.Persistence.Seed;

// D-V2-33: as porções vêm só de fontes automáticas (veja Data/SOURCE-PORCOES.md). Cada arquivo tem a sua marca na
// tb_seed_run: sem mudança no CSV, a subida da API não relê nada. Uma porção que já existe (mesmo nome) não entra de novo.
public sealed class FoodPortionSeeder(NogVitaDbContext context, ILogger<FoodPortionSeeder> logger)
{
    public const string TacoSeedName = "porcoes-taco-ibge";
    public const string OpenFoodFactsSeedName = "porcoes-open-food-facts";

    internal const string TacoResource = "NogVita.Infrastructure.Seed.taco_porcoes.csv";
    internal const string OpenFoodFactsResource = "NogVita.Infrastructure.Seed.off_porcoes.csv";

    private const int BatchSize = 1000;

    // Depois do TacoFoodSeeder, na subida da API.
    public Task SeedTacoAsync(CancellationToken cancellationToken = default) =>
        SeedAsync(TacoSeedName, TacoResource, FoodSource.Taco, FoodPortionSource.Ibge,
            row => (row["numero_alimento"], row["porcao"], row["gramas"]), cancellationToken);

    // No fim da importação em massa do Open Food Facts (OpenFoodFactsImportHostedService): só então os produtos existem.
    public Task SeedOpenFoodFactsAsync(CancellationToken cancellationToken = default) =>
        SeedAsync(OpenFoodFactsSeedName, OpenFoodFactsResource, FoodSource.OpenFoodFacts, FoodPortionSource.OpenFoodFacts,
            row => (row["code"], FoodPortion.PackageName(row["serving_label"]), row["serving_grams"]), cancellationToken);

    private async Task SeedAsync(
        string seedName,
        string resource,
        FoodSource foodSource,
        FoodPortionSource portionSource,
        Func<IReadOnlyDictionary<string, string>, (string Reference, string Name, string Grams)> map,
        CancellationToken cancellationToken)
    {
        var hash = ComputeHash(resource);
        var run = await context.SeedRuns.FindAsync([seedName], cancellationToken);

        if (run?.ContentHash == hash)
        {
            logger.LogInformation("Porções ({Seed}): este arquivo já foi importado. Nada a fazer.", seedName);
            return;
        }

        var foodIds = await context.Foods
            .AsNoTracking()
            .Where(f => f.Source == foodSource)
            .Select(f => new { f.SourceReference, f.Id })
            .ToDictionaryAsync(f => f.SourceReference, f => f.Id, cancellationToken);

        var existing = (await context.Set<FoodPortion>()
                .AsNoTracking()
                .Where(p => foodIds.Values.Contains(p.FoodId))
                .Select(p => new { p.FoodId, p.Name })
                .ToListAsync(cancellationToken))
            .Select(p => (p.FoodId, p.Name.ToUpperInvariant()))
            .ToHashSet();

        var added = 0;
        var batch = new List<FoodPortion>(BatchSize);

        foreach (var row in ReadRows(resource))
        {
            var (reference, name, grams) = map(row);

            if (!foodIds.TryGetValue(reference, out var foodId) || !existing.Add((foodId, name.Trim().ToUpperInvariant())))
            {
                continue;
            }

            try
            {
                batch.Add(new FoodPortion(foodId, name, decimal.Parse(grams, CultureInfo.InvariantCulture), portionSource));
            }
            catch (DomainException exception)
            {
                logger.LogWarning("Porções ({Seed}): a linha de {Reference} foi recusada: {Reason}", seedName, reference, exception.Message);
                continue;
            }

            if (batch.Count == BatchSize)
            {
                added += await SaveBatchAsync(batch, cancellationToken);
            }
        }

        if (batch.Count > 0)
        {
            added += await SaveBatchAsync(batch, cancellationToken);
        }

        // A marca só no fim: uma importação interrompida continua na próxima subida (as porções repetidas são puladas).
        if (run is null)
        {
            context.SeedRuns.Add(new SeedRun(seedName, hash, DateTime.UtcNow));
        }
        else
        {
            context.SeedRuns.Attach(run);
            run.Complete(hash, DateTime.UtcNow);
        }

        await context.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Porções ({Seed}): {Added} porções importadas.", seedName, added);
    }

    private async Task<int> SaveBatchAsync(List<FoodPortion> batch, CancellationToken cancellationToken)
    {
        context.Set<FoodPortion>().AddRange(batch);
        await context.SaveChangesAsync(cancellationToken);
        context.ChangeTracker.Clear();

        var count = batch.Count;
        batch.Clear();
        return count;
    }

    internal static IEnumerable<IReadOnlyDictionary<string, string>> ReadRows(string resource)
    {
        using var stream = OpenResource(resource);
        using var parser = new TextFieldParser(stream, Encoding.UTF8)
        {
            TextFieldType = FieldType.Delimited,
            HasFieldsEnclosedInQuotes = true
        };
        parser.SetDelimiters(",");

        var header = parser.ReadFields() ?? throw new InvalidOperationException($"O CSV {resource} está vazio.");

        while (!parser.EndOfData)
        {
            var fields = parser.ReadFields();
            if (fields is null || fields.Length != header.Length)
            {
                continue;
            }

            yield return header.Zip(fields).ToDictionary(p => p.First, p => p.Second);
        }
    }

    private static string ComputeHash(string resource)
    {
        using var stream = OpenResource(resource);
        return Convert.ToHexString(SHA256.HashData(stream));
    }

    private static Stream OpenResource(string resource) =>
        Assembly.GetExecutingAssembly().GetManifestResourceStream(resource)
            ?? throw new InvalidOperationException($"O arquivo embutido '{resource}' não foi encontrado.");
}
