using Microsoft.EntityFrameworkCore;
using NogVita.Application.Anamneses;
using NogVita.Application.Common.Pagination;
using NogVita.Domain.Anamneses;

namespace NogVita.Infrastructure.Persistence.Queries;

internal sealed class AnamnesisQueries(NogVitaDbContext context) : IAnamnesisQueries
{
    // O número de perguntas é contado na memória: no SQL, o Npgsql traduz o .Count do jsonb para questions ->> 'Count'.
    public async Task<PagedResponse<QuestionnaireSummaryResponse>> ListQuestionnairesAsync(
        Guid nutritionistId, AnamnesisListRequest request, CancellationToken cancellationToken = default)
    {
        var page = await context.Questionnaires
            .AsNoTracking()
            .Where(q => q.NutritionistId == nutritionistId)
            .OrderBy(q => q.Title)
            .ThenBy(q => q.Id)
            .Select(q => new { q.Id, q.Title, q.Questions, UpdatedAt = q.UpdatedAt ?? q.CreatedAt })
            .ToPagedResponseAsync(request, cancellationToken);

        return page.Map(q => new QuestionnaireSummaryResponse(q.Id, q.Title, q.Questions.Count, q.UpdatedAt));
    }

    public Task<QuestionnaireResponse?> GetQuestionnaireAsync(
        Guid nutritionistId, Guid questionnaireId, CancellationToken cancellationToken = default) =>
        context.Questionnaires
            .AsNoTracking()
            .Where(q => q.Id == questionnaireId && q.NutritionistId == nutritionistId)
            .Select(q => new QuestionnaireResponse(q.Id, q.Title, q.Questions, q.CreatedAt, q.UpdatedAt))
            .FirstOrDefaultAsync(cancellationToken);

    public Task<PagedResponse<AnamnesisSummaryResponse>> ListByNutritionistAndPatientAsync(
        Guid nutritionistId, Guid patientId, AnamnesisListRequest request, CancellationToken cancellationToken = default) =>
        Summaries(context.Anamneses
                .Where(a => a.NutritionistId == nutritionistId && a.PatientId == patientId)
                .OrderByDescending(a => a.CreatedAt)
                .ThenBy(a => a.Id))
            .ToPagedResponseAsync(request, cancellationToken);

    // As pendentes primeiro: é o que o paciente tem para fazer.
    public Task<PagedResponse<AnamnesisSummaryResponse>> ListByPatientAsync(
        Guid patientId, AnamnesisListRequest request, CancellationToken cancellationToken = default) =>
        Summaries(context.Anamneses
                .Where(a => a.PatientId == patientId)
                .OrderByDescending(a => a.Status == AnamnesisStatus.Pending)
                .ThenByDescending(a => a.CreatedAt)
                .ThenBy(a => a.Id))
            .ToPagedResponseAsync(request, cancellationToken);

    public Task<AnamnesisResponse?> GetByNutritionistAsync(
        Guid nutritionistId, Guid anamnesisId, CancellationToken cancellationToken = default) =>
        Details(context.Anamneses.Where(a => a.Id == anamnesisId && a.NutritionistId == nutritionistId))
            .FirstOrDefaultAsync(cancellationToken);

    public Task<AnamnesisResponse?> GetByPatientAsync(
        Guid patientId, Guid anamnesisId, CancellationToken cancellationToken = default) =>
        Details(context.Anamneses.Where(a => a.Id == anamnesisId && a.PatientId == patientId))
            .FirstOrDefaultAsync(cancellationToken);

    private IQueryable<AnamnesisSummaryResponse> Summaries(IQueryable<Anamnesis> anamneses) =>
        from a in anamneses.AsNoTracking()
        join patient in context.Users on a.PatientId equals patient.Id
        join nutritionist in context.Users on a.NutritionistId equals nutritionist.Id
        select new AnamnesisSummaryResponse(
            a.Id, a.Title, a.Status, a.PatientId, patient.Name, a.NutritionistId, nutritionist.Name, a.CreatedAt, a.SubmittedAtUtc);

    private IQueryable<AnamnesisResponse> Details(IQueryable<Anamnesis> anamneses) =>
        from a in anamneses.AsNoTracking()
        join patient in context.Users on a.PatientId equals patient.Id
        join nutritionist in context.Users on a.NutritionistId equals nutritionist.Id
        select new AnamnesisResponse(
            a.Id, a.Title, a.Status, a.PatientId, patient.Name, a.NutritionistId, nutritionist.Name, a.CreatedAt, a.SubmittedAtUtc,
            a.Questions, a.Answers);
}
