using Microsoft.EntityFrameworkCore;
using NogVita.Application.Common.Pagination;
using NogVita.Application.Foods;
using NogVita.Domain.Foods;

namespace NogVita.Infrastructure.Persistence.Queries;

internal sealed class FoodQueries(NogVitaDbContext context) : IFoodQueries
{
    private const int MaxSearchWords = 8;

    // Hífen e pontuação também separam: a base escreve "Coca-Cola" e "Coca Cola", e "coca-cola zero" precisa achar as duas.
    private static readonly char[] WordSeparators = [' ', '\t', '-', ',', '.', '/', '(', ')'];

    public Task<PagedResponse<FoodResponse>> SearchAsync(
        FoodSearchRequest request,
        bool includeInactive = false,
        CancellationToken cancellationToken = default) =>
        BuildSearch(request, includeInactive)
            .Select(ToResponse())
            .ToPagedResponseAsync(request, cancellationToken);

    // Separada do SearchAsync para um teste conferir o SQL gerado (o índice só é usado com a mesma expressão dele).
    internal IQueryable<Food> BuildSearch(FoodSearchRequest request, bool includeInactive)
    {
        var query = context.Foods.AsNoTracking();

        if (!includeInactive)
        {
            query = query.Where(f => f.IsActive);
        }

        var words = SearchWords(request.Search);

        // Cada palavra precisa aparecer no nome, em qualquer ordem e posição: "coca zero" acha "Coca-Cola Zero Açúcar".
        // D191: f_unaccent(name) é exatamente a expressão do índice de trigramas ix_food_name_trgm.
        foreach (var word in words)
        {
            var pattern = $"%{QueryableExtensions.EscapeLikePattern(word)}%";
            query = query.Where(f => EF.Functions.ILike(NogVitaDbFunctions.Unaccent(f.Name), NogVitaDbFunctions.Unaccent(pattern), @"\"));
        }

        if (request.Source is not null)
        {
            query = query.Where(f => f.Source == request.Source);
        }

        // A relevância (Fase 27): antes, a ordem era só alfabética, e o arroz da TACO aparecia na posição 75 de 502.
        // 1) o nome que começa com a primeira palavra buscada ("Arroz, integral..." antes de "Bolinho de arroz");
        // 2) a TACO, que é a referência para montar um plano, antes dos produtos industrializados;
        // 3) o nome mais curto, que costuma ser o mais genérico ("Arroz, tipo 1, cozido" antes de uma marca com sabor);
        // 4) a ordem alfabética, e o Id para a paginação ser estável.
        var startsWith = $"{QueryableExtensions.EscapeLikePattern(words[0])}%";

        return query
            .OrderBy(f => EF.Functions.ILike(NogVitaDbFunctions.Unaccent(f.Name), NogVitaDbFunctions.Unaccent(startsWith), @"\") ? 0 : 1)
            .ThenBy(f => f.Source == FoodSource.Taco ? 0 : 1)
            .ThenBy(f => f.Name.Length)
            .ThenBy(f => f.Name)
            .ThenBy(f => f.Id);
    }

    internal static IReadOnlyList<string> SearchWords(string search)
    {
        var words = search
            .Split(WordSeparators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(MaxSearchWords)
            .ToList();

        // Um texto só de separadores (como "--") vira a busca pelo texto original, para não listar a base inteira.
        return words.Count > 0 ? words : [search.Trim()];
    }

    public Task<FoodResponse?> GetByIdAsync(Guid id, bool includeInactive, CancellationToken cancellationToken = default)
    {
        var query = context.Foods
            .AsNoTracking()
            .Where(f => f.Id == id);

        if (!includeInactive)
        {
            query = query.Where(f => f.IsActive);
        }

        return query
            .Select(ToResponse())
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static System.Linq.Expressions.Expression<Func<Food, FoodResponse>> ToResponse() =>
        f => new FoodResponse(
            f.Id,
            f.Name,
            f.Brand,
            f.Barcode,
            f.Category,
            f.Source,
            f.SourceReference,
            f.IsActive,
            f.Nutrients.EnergyKcal,
            f.Nutrients.Protein,
            f.Nutrients.Carbohydrate,
            f.Nutrients.Fat,
            f.Nutrients.Fiber,
            f.Nutrients.SodiumMg);
}
