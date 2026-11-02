using Microsoft.EntityFrameworkCore;
using NogVita.Application.Abstractions;
using NogVita.Application.Common.Pagination;
using NogVita.Application.FollowUps;
using NogVita.Application.Photos;
using NogVita.Domain.FollowUps;

namespace NogVita.Infrastructure.Persistence.Queries;

// As fotos: o banco devolve o CAMINHO de cada item (a URL não existe no banco), e as URLs são montadas depois,
// a página inteira de uma vez. O PhotoUrl dos itens vai null dentro da consulta: uma árvore de expressão
// não aceita argumento opcional omitido (CS0854).
internal sealed class FollowUpQueries(NogVitaDbContext context, IFileStorage fileStorage) : IFollowUpQueries
{
    public async Task<PagedResponse<PatientRequestItem>> ListPatientRequestsAsync(
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
            select new
            {
                Item = new PatientRequestItem(
                    r.Id,
                    nutritionist.Id,
                    nutritionist.Name,
                    r.Status,
                    r.Message,
                    r.CreatedAt,
                    r.RespondedAtUtc,
                    null),
                nutritionist.NutritionistProfile!.PhotoPath
            };

        var page = await query.ToPagedResponseAsync(request, cancellationToken);

        // A foto do nutricionista é pública: a URL é montada aqui, sem chamar o Supabase.
        return page.Map(i => i.Item with { NutritionistPhotoUrl = fileStorage.NutritionistPhotoUrl(i.PhotoPath) });
    }

    public async Task<PagedResponse<NutritionistRequestItem>> ListNutritionistRequestsAsync(
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
            select new
            {
                Item = new NutritionistRequestItem(
                    r.Id,
                    patient.Id,
                    patient.Name,
                    r.Status,
                    r.Message,
                    r.CreatedAt,
                    r.RespondedAtUtc,
                    null),
                // D-V2-11: a foto do paciente só nos pedidos pendentes. Um pedido recusado ou cancelado não dá acesso.
                // A coluna é comparada direto (vira um CASE no SQL): uma propriedade calculada não se traduz.
                PhotoPath = r.Status == NutritionistRequestStatus.Pending ? patient.PatientProfile!.PhotoPath : null
            };

        var page = await query.ToPagedResponseAsync(request, cancellationToken);
        var urls = await fileStorage.SignPatientPhotosAsync(page.Items.Select(i => i.PhotoPath), cancellationToken);

        return page.Map(i => i.Item with { PatientPhotoUrl = urls.UrlFor(i.PhotoPath) });
    }

    public async Task<PagedResponse<MyPatientItem>> ListMyPatientsAsync(
        Guid nutritionistId, MyPatientsListRequest request, CancellationToken cancellationToken = default)
    {
        var relationships = context.CareRelationships
            .AsNoTracking()
            .Where(c => c.NutritionistId == nutritionistId && c.EndedAtUtc == null);

        var query =
            from c in relationships
            join patient in context.Users on c.PatientId equals patient.Id
            where patient.DeactivatedAtUtc == null // o mesmo critério do CareAccessService: paciente bloqueado some da lista
            select new { c, patient };

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var pattern = $"%{QueryableExtensions.EscapeLikePattern(request.Search.Trim())}%";
            query = query.Where(x => EF.Functions.ILike(x.patient.Name, pattern, @"\"));
        }

        // 1. O banco devolve o item e o caminho da foto.
        var page = await query
            .OrderBy(x => x.patient.Name)
            .ThenBy(x => x.patient.Id)
            .Select(x => new
            {
                Item = new MyPatientItem(x.patient.Id, x.patient.Name, x.patient.PatientProfile!.Goal, x.c.StartedAtUtc, null),
                x.patient.PatientProfile.PhotoPath
            })
            .ToPagedResponseAsync(request, cancellationToken);

        // 2. Todas as URLs da página numa chamada só. 3. Cada item recebe a sua.
        var urls = await fileStorage.SignPatientPhotosAsync(page.Items.Select(i => i.PhotoPath), cancellationToken);

        return page.Map(i => i.Item with { PhotoUrl = urls.UrlFor(i.PhotoPath) });
    }
}
