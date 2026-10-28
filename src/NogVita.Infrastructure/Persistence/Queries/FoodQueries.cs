using Microsoft.EntityFrameworkCore;
using NogVita.Application.Common.Pagination;
using NogVita.Application.Foods;
using NogVita.Domain.Foods;
using NogVita.Infrastructure.Queries;

namespace NogVita.Infrastructure.Persistence.Queries;

internal sealed class FoodQueries(NogVitaDbContext context) : IFoodQueries
{
    private const int MaxSearchWords = 8;

    // Hífen e pontuação também separam: a base escreve "Coca-Cola" e "Coca Cola", e "coca-cola zero" precisa achar as duas.
    private static readonly char[] WordSeparators = [' ', '\t', '-', ',', '.', '/', '(', ')'];

    public Task<PagedResponse<FoodResponse>> SearchAsync(
        FoodSearchRequest request,
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var query = context.Foods.AsNoTracking();

        if (!includeInactive)
        {
            query = query.Where(f => f.IsActive);
        }

        // Cada palavra precisa aparecer no nome, em qualquer ordem e posição: "coca zero" acha "Coca-Cola Zero Açúcar".
        foreach (var word in SearchWords(request.Search))
        {
            var pattern = $"%{QueryableExtensions.EscapeLikePattern(word)}%";
            query = query.Where(f => EF.Functions.ILike(EF.Functions.Unaccent(f.Name), EF.Functions.Unaccent(pattern), @"\"));
        }

        if (request.Source is not null)
        {
            query = query.Where(f => f.Source == request.Source);
        }

        return query
            .OrderBy(f => f.Name)
            .ThenBy(f => f.Id)
            .Select(ToResponse())
            .ToPagedResponseAsync(request, cancellationToken);
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