using Microsoft.EntityFrameworkCore;
using NogVita.Application.Admin;
using NogVita.Application.Common.Pagination;
using NogVita.Domain.Users;
using NogVita.Infrastructure.Persistence;

namespace NogVita.Infrastructure.Queries;

public sealed class AdminUserQueries(NogVitaDbContext context) : IAdminUserQueries
{
    public Task<PagedResponse<AdminUserSummary>> ListUsersAsync(AdminUserListRequest request, CancellationToken cancellationToken = default)
    {
        return ApplyFilters(context.Users.AsNoTracking(), request)
            .Select(u => new AdminUserSummary(
                u.Id,
                u.Name,
                u.Email,
                u.IsActive,
                u.IsAdmin,
                u.PatientProfile != null,
                u.NutritionistProfile != null && u.NutritionistProfile.IsActive,
                u.CreatedAt))
            .ToPagedResponseAsync(request, cancellationToken);
    }

    public Task<AdminUserDetail?> GetUserAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return context.Users
            .AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => new AdminUserDetail(
                u.Id,
                u.Name,
                u.Email,
                u.Cpf.Formatted,
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

    private static IQueryable<User> ApplyFilters(IQueryable<User> query, AdminUserListRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var pattern = $"%{EscapeLikePattern(request.Search.Trim())}%";
            query = query.Where(u =>
                EF.Functions.ILike(u.Name, pattern, @"\") ||
                EF.Functions.ILike(u.Email, pattern, @"\"));
        }

        if (request.IsActive is not null)
            query = query.Where(u => u.IsActive == request.IsActive);

        return query
            .OrderByDescending(u => u.CreatedAt)
            .ThenBy(u => u.Id);
    }
    private static string EscapeLikePattern(string value) =>
        value.Replace(@"\", @"\\").Replace("%", @"\%").Replace("_", @"\_");

    public Task<PagedResponse<AdminPatientSummary>> ListPatientsAsync(AdminUserListRequest request, CancellationToken cancellationToken = default)
    {
        var patients = context.Users.AsNoTracking().Where(u => u.PatientProfile != null);

        return ApplyFilters(patients, request)
            .Select(u => new AdminPatientSummary(
                u.Id,
                u.Name,
                u.Email,
                u.IsActive,
                u.PatientProfile!.BirthDate,
                u.PatientProfile.Goal.ToString(),
                u.CreatedAt))
            .ToPagedResponseAsync(request, cancellationToken);
    }

    public Task<PagedResponse<AdminNutritionistSummary>> ListNutritionistsAsync(AdminUserListRequest request, CancellationToken cancellationToken = default)
    {
        var nutritionists = context.Users.AsNoTracking().Where(u => u.NutritionistProfile != null);

        return ApplyFilters(nutritionists, request)
            .Select(u => new AdminNutritionistSummary(
                u.Id,
                u.Name,
                u.Email,
                u.IsActive,
                u.NutritionistProfile!.CrnRegion,
                u.NutritionistProfile.CrnNumber,
                u.NutritionistProfile.IsActive,
                u.CreatedAt))
            .ToPagedResponseAsync(request, cancellationToken);
    }

}