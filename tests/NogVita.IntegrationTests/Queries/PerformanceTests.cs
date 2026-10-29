using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NogVita.Domain.Foods;
using NogVita.Infrastructure.Persistence.Seed;
using NogVita.IntegrationTests.Infrastructure;

namespace NogVita.IntegrationTests.Queries;

// Fase 27: o que a revisão de performance mudou, conferido contra o Postgres real.
public class PerformanceTests(NogVitaApiFixture fixture)
{
    private readonly TestAccounts _accounts = new(fixture);
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    // D191: o índice existe, e a busca do jeito que o EF a escreve consegue usá-lo. Com só ~600 alimentos no banco de
    // teste, o Postgres preferiria ler a tabela; o enable_seqscan = off força a pergunta "o índice serve para esta consulta?".
    [Fact]
    public async Task Food_Search_Should_Be_Able_To_Use_The_Trigram_Index()
    {
        await using var context = fixture.CreateDbContext();
        await context.Database.OpenConnectionAsync(_ct);
        await context.Database.ExecuteSqlRawAsync("SET enable_seqscan = off", _ct);

        var plan = await context.Database.SqlQueryRaw<string>(
                @"EXPLAIN SELECT count(*) FROM tb_food t WHERE t.is_active AND public.f_unaccent(t.name) ILIKE public.f_unaccent('%arroz%') ESCAPE '\'")
            .ToListAsync(_ct);

        Assert.Contains(plan, line => line.Contains("ix_food_name_trgm"));
    }

    // A relevância: os alimentos da TACO que começam com a palavra vêm antes de um produto do Open Food Facts chamado
    // só "Arroz", mesmo ele tendo o nome mais curto. Antes, a ordem era só alfabética.
    [Fact]
    public async Task Taco_Foods_Starting_With_The_Word_Should_Come_First()
    {
        await using (var context = fixture.CreateDbContext())
        {
            context.Foods.Add(Food.FromOpenFoodFacts(
                Random.Shared.NextInt64(1_000_000_000_000, 9_999_999_999_999).ToString(), "Arroz", null,
                new NutrientsPer100g(350m, 7m, 78m, 1m, 2m, 5m)));
            await context.SaveChangesAsync(_ct);
        }

        var nutritionist = _accounts.As(await _accounts.NewNutritionistAsync());
        var page = await nutritionist.GetFromJsonAsync<JsonElement>("/api/v1/foods?search=arroz&pageSize=100", TestAccounts.Json, _ct);
        var items = page.GetProperty("items").EnumerateArray()
            .Select(i => (Name: i.GetProperty("name").GetString()!, Source: i.GetProperty("source").GetString()!))
            .ToList();

        var tacoStartingWithArroz = items.Count(i => i.Source == "Taco" && i.Name.StartsWith("Arroz", StringComparison.OrdinalIgnoreCase));
        Assert.True(tacoStartingWithArroz > 0);
        Assert.All(items.Take(tacoStartingWithArroz), i =>
        {
            Assert.Equal("Taco", i.Source);
            Assert.StartsWith("Arroz", i.Name);
        });
        Assert.Contains(items.Skip(tacoStartingWithArroz), i => i.Source == "OpenFoodFacts" && i.Name == "Arroz");
    }

    // D192: com a marca do mesmo arquivo, a importação não relê o CSV nem busca os 22.728 códigos.
    [Fact]
    public async Task Import_Should_Skip_When_The_Same_File_Was_Already_Imported()
    {
        await using var context = fixture.CreateDbContext();
        var hash = OpenFoodFactsBulkSeeder.CurrentFileHash();
        var run = await context.SeedRuns.FindAsync([OpenFoodFactsBulkSeeder.SeedName], _ct);
        if (run is null)
        {
            context.SeedRuns.Add(new SeedRun(OpenFoodFactsBulkSeeder.SeedName, hash, DateTime.UtcNow));
            await context.SaveChangesAsync(_ct);
        }

        var before = await context.Foods.CountAsync(f => f.Source == FoodSource.OpenFoodFacts, _ct);
        var watch = Stopwatch.StartNew();

        await new OpenFoodFactsBulkSeeder(context, NullLogger<OpenFoodFactsBulkSeeder>.Instance).SeedAsync(_ct);

        watch.Stop();
        Assert.Equal(before, await context.Foods.CountAsync(f => f.Source == FoodSource.OpenFoodFacts, _ct));
        Assert.True(watch.Elapsed < TimeSpan.FromSeconds(2), $"A passada sem nada a fazer levou {watch.Elapsed.TotalMilliseconds:N0} ms.");
    }
}
