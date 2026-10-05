using Microsoft.EntityFrameworkCore;
using NogVita.Application.Admin;
using NogVita.Application.Common.Pagination;
using NogVita.Infrastructure.Persistence;

namespace NogVita.Infrastructure.Queries;

public sealed class AdminUserQueries(NogVitaDbContext context) : IAdminUserQueries
{
    public async Task<PagedResponse<AdminUserSummary>> ListUsersAsync(AdminUserListRequest request, CancellationToken cancellationToken = default)
    {
        var query = context.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var pattern = $"%{EscapeLikePattern(request.Search.Trim())}%";
            query = query.Where(u =>
                EF.Functions.ILike(u.Name, pattern, @"\") ||
                EF.Functions.ILike(u.Email, pattern, @"\"));
        }

        if (request.IsActive is not null)
            query = query.Where(u => u.IsActive == request.IsActive);

        var totalItems = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(u => u.CreatedAt)
            .ThenBy(u => u.Id)
            .Skip(request.Skip)
            .Take(request.PageSize)
            .Select(u => new AdminUserSummary(
                u.Id,
                u.Name,
                u.Email,
                u.IsActive,
                u.IsAdmin,
                u.PatientProfile != null,
                u.NutritionistProfile != null && u.NutritionistProfile.IsActive,
                u.CreatedAt))
            .ToListAsync(cancellationToken);

        return new PagedResponse<AdminUserSummary>(items, request.Page, request.PageSize, totalItems);
    }

    public Task<AdminUserDetail?> GetUserAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return context.Users
            .Where(u => u.Id == id)
            .Select(u => new AdminUserDetail(
                u.Id,
                u.Name,
                u.Email,
                u.Cpf.ToString(),
                u.IsActive,
                u.IsAdmin,
                u.PatientProfile != null ? new AdminPatientInfo(
                    u.PatientProfile.BirthDate,
                    u.PatientProfile.BiologicalSex.ToString(),
                    u.PatientProfile.HeightInCm,
                    u.PatientProfile.Goal.ToString()
                ) : null,
                u.NutritionistProfile != null ? new AdminNutritionistInfo(
                    u.NutritionistProfile.CrnRegion,
                    u.NutritionistProfile.CrnNumber,
                    u.NutritionistProfile.IsActive
                ) : null,
                u.CreatedAt,
                u.UpdatedAt
            ))
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static string EscapeLikePattern(string value) =>
        value.Replace(@"\", @"\\").Replace("%", @"\%").Replace("_", @"\_");
}