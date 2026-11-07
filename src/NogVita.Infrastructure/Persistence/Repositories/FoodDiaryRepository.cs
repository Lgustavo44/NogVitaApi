using Microsoft.EntityFrameworkCore;
using NogVita.Application.Abstractions;
using NogVita.Domain.FoodDiary;

namespace NogVita.Infrastructure.Persistence.Repositories;

internal sealed class FoodDiaryRepository(NogVitaDbContext context) : IFoodDiaryRepository
{
    public Task<DiaryDay?> GetDayAsync(Guid patientId, DateOnly date, CancellationToken cancellationToken = default) =>
        context.DiaryDays.FirstOrDefaultAsync(d => d.PatientId == patientId && d.Date == date, cancellationToken);

    public async Task<IReadOnlyList<DiaryDay>> ListDaysAsync(Guid patientId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default) =>
        await context.DiaryDays
            .AsNoTracking()
            .Where(d => d.PatientId == patientId && d.Date >= from && d.Date <= to)
            .OrderByDescending(d => d.Date)
            .ToListAsync(cancellationToken);

    public void Add(DiaryDay day) => context.DiaryDays.Add(day);

    public Task<WeeklyCheckIn?> GetWeekAsync(Guid patientId, DateOnly weekStart, CancellationToken cancellationToken = default) =>
        context.WeeklyCheckIns.FirstOrDefaultAsync(w => w.PatientId == patientId && w.WeekStart == weekStart, cancellationToken);

    public Task<WeeklyCheckIn?> GetWeekByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.WeeklyCheckIns.FirstOrDefaultAsync(w => w.Id == id, cancellationToken);

    public async Task<IReadOnlyList<WeeklyCheckIn>> ListWeeksAsync(Guid patientId, int take, CancellationToken cancellationToken = default) =>
        await context.WeeklyCheckIns
            .AsNoTracking()
            .Where(w => w.PatientId == patientId)
            .OrderByDescending(w => w.WeekStart)
            .Take(take)
            .ToListAsync(cancellationToken);

    // Só de quem o nutricionista acompanha agora: um paciente que saiu do acompanhamento não aparece mais na lista.
    public async Task<IReadOnlyList<(WeeklyCheckIn CheckIn, string PatientName)>> ListAwaitingFeedbackAsync(
        Guid nutritionistId, CancellationToken cancellationToken = default)
    {
        var rows = await (
                from w in context.WeeklyCheckIns.AsNoTracking()
                join patient in context.Users on w.PatientId equals patient.Id
                where w.NutritionistId == nutritionistId
                      && w.Feedback == null
                      && patient.IsActive
                      && context.CareRelationships.Any(r =>
                          r.PatientId == w.PatientId && r.NutritionistId == nutritionistId && r.EndedAtUtc == null)
                orderby w.WeekStart descending
                select new { CheckIn = w, patient.Name })
            .ToListAsync(cancellationToken);

        return rows.Select(r => (r.CheckIn, r.Name)).ToList();
    }

    public void Add(WeeklyCheckIn checkIn) => context.WeeklyCheckIns.Add(checkIn);
}
