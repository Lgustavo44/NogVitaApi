using Microsoft.EntityFrameworkCore;
using NogVita.Application.Common.Pagination;

namespace NogVita.Infrastructure.Queries;

internal static class QueryableExtensions
{
    internal static string EscapeLikePattern(string value) =>
    value.Replace(@"\", @"\\").Replace("%", @"\%").Replace("_", @"\_");

    public static async Task<PagedResponse<T>> ToPagedResponseAsync<T>(
        this IQueryable<T> query,
        PageRequest request,
        CancellationToken cancellationToken)
    {
        var totalItems = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip(request.Skip)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResponse<T>(items, request.Page, request.PageSize, totalItems);
    }
}