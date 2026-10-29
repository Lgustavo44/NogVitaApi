using Microsoft.EntityFrameworkCore;
using NogVita.Application.Foods;
using NogVita.Infrastructure.Persistence;
using NogVita.Infrastructure.Persistence.Queries;
using NogVita.Infrastructure.Persistence.Seed;

namespace NogVita.UnitTests.Infrastructure.Foods;

// Fase 27: o SQL que o EF gera para a busca, sem precisar de banco (o ToQueryString não abre conexão).
public class FoodSearchSqlTests
{
    private static string SearchSql(string search)
    {
        var options = new DbContextOptionsBuilder<NogVitaDbContext>()
            .UseNpgsql("Host=sem-banco")
            .UseSnakeCaseNamingConvention()
            .Options;

        using var context = new NogVitaDbContext(options);
        return new FoodQueries(context).BuildSearch(new FoodSearchRequest { Search = search }, includeInactive: false).ToQueryString();
    }

    // D191: o índice ix_food_name_trgm é sobre f_unaccent(name). Ele só é usado se a consulta usar exatamente
    // a mesma expressão; com o unaccent comum, o Postgres voltaria a ler a tabela inteira.
    [Fact]
    public void Search_Should_Use_The_Same_Expression_As_The_Index()
    {
        var sql = SearchSql("arroz integral");

        Assert.Contains("public.f_unaccent(t.name) ILIKE", sql);
        Assert.DoesNotContain(" unaccent(t.name)", sql);
    }

    // A relevância: começa com a primeira palavra, depois a TACO, depois o nome mais curto, e só então a ordem alfabética.
    [Fact]
    public void Search_Should_Order_By_Relevance()
    {
        var orderBy = SearchSql("arroz")[SearchSql("arroz").IndexOf("ORDER BY", StringComparison.Ordinal)..];

        var startsWith = orderBy.IndexOf("ILIKE", StringComparison.Ordinal);
        var taco = orderBy.IndexOf("'Taco'", StringComparison.Ordinal);
        var length = orderBy.IndexOf("length(t.name)", StringComparison.Ordinal);
        var name = orderBy.IndexOf("t.name,", StringComparison.Ordinal);

        Assert.True(startsWith > 0 && startsWith < taco && taco < length && length < name, orderBy);
    }
}

public class OpenFoodFactsFileHashTests
{
    // D192: o hash identifica o arquivo. O mesmo arquivo dá sempre o mesmo hash (e a importação pula),
    // um arquivo novo dá outro (e a importação roda).
    [Fact]
    public void Hash_Should_Be_A_Stable_Sha256()
    {
        var first = OpenFoodFactsBulkSeeder.CurrentFileHash();

        Assert.Matches("^[0-9A-F]{64}$", first);
        Assert.Equal(first, OpenFoodFactsBulkSeeder.CurrentFileHash());
    }
}
