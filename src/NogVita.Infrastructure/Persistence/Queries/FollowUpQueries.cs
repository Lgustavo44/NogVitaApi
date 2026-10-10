using Microsoft.EntityFrameworkCore;
using NogVita.Application.Common.Pagination;
using NogVita.Application.FollowUps;
using NogVita.Domain.FollowUps;
using NogVita.Infrastructure.Queries;

namespace NogVita.Infrastructure.Persistence.Queries;

internal sealed class FollowUpQueries(NogVitaDbContext context) : IFollowUpQueries
{
    public Task<PagedResponse<PatientRequestItem>> ListPatientRequestsAsync(
        Guid patientId, NutritionistRequestListRequest request, CancellationToken cancellationToken = default)
    {
        var requests = context.NutritionistRequests.AsNoTracking().Where(r => r.PatientId == patientId);

        if (request.Status is not null)
        {
            requests = requests.Where(r => r.Status == request.Status);
        }

        var query =
            from r in requests
            join nutritionist in context.Users on r.NutritionistId equals nutritionist.Id
            orderby r.CreatedAt descending, r.Id
            select new PatientRequestItem(
                r.Id,
                nutritionist.Id,
                nutritionist.Name,
                r.Status,
                r.Message,
                r.CreatedAt,
                r.RespondedAtUtc);

        return query.ToPagedResponseAsync(request, cancellationToken);
    }

    public Task<PagedResponse<NutritionistRequestItem>> ListNutritionistRequestsAsync(
        Guid nutritionistId, NutritionistRequestListRequest request, CancellationToken cancellationToken = default)
    {
        var requests = context.NutritionistRequests.AsNoTracking().Where(r => r.NutritionistId == nutritionistId);

        if (request.Status is not null)
        {
            requests = requests.Where(r => r.Status == request.Status);
        }

        var query =
            from r in requests
            join patient in context.Users on r.PatientId equals patient.Id
            orderby r.CreatedAt descending, r.Id
            select new NutritionistRequestItem(
                r.Id,
                patient.Id,
                patient.Name,
                r.Status,
                r.Message,
                r.CreatedAt,
                r.RespondedAtUtc);

        return query.ToPagedResponseAsync(request, cancellationToken);
    }

    public Task<PagedResponse<MyPatientItem>> ListMyPatientsAsync(
        Guid nutritionistId, MyPatientsListRequest request, CancellationToken cancellationToken = default)
    {
        var relationships = context.CareRelationships
            .AsNoTracking()
            .Where(c => c.NutritionistId == nutritionistId && c.EndedAtUtc == null);

        var query =
            from c in relationships
            join patient in context.Users on c.PatientId equals patient.Id
            select new { c, patient };

        // TODO 2: se request.Search tiver texto → filtrar por x.patient.Name com ILike
        //         (o EscapeLikePattern e o @"\", como no NutritionistQueries)
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var pattern = $"%{QueryableExtensions.EscapeLikePattern(request.Search.Trim())}%";
            query = query.Where(x => EF.Functions.ILike(x.patient.Name, pattern, @"\"));
        }



        return query
            .OrderBy(x => x.patient.Name)
            .ThenBy(x => x.patient.Id)
            .Select(x => new MyPatientItem(
                x.patient.Id,
                x.patient.Name,
                x.patient.PatientProfile!.Goal,
                x.c.StartedAtUtc))
            .ToPagedResponseAsync(request, cancellationToken);
    }
}