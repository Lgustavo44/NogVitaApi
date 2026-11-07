using NogVita.Application.Abstractions;
using NogVita.Domain.FoodDiary;

namespace NogVita.UnitTests.Fakes;

public sealed class FakeFoodDiaryRepository : IFoodDiaryRepository
{
    public List<DiaryDay> Days { get; } = [];
    public List<WeeklyCheckIn> Weeks { get; } = [];

    public Task<DiaryDay?> GetDayAsync(Guid patientId, DateOnly date, CancellationToken cancellationToken = default) =>
        Task.FromResult(Days.FirstOrDefault(d => d.PatientId == patientId && d.Date == date));

    public Task<IReadOnlyList<DiaryDay>> ListDaysAsync(Guid patientId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<DiaryDay>>(Days.Where(d => d.PatientId == patientId && d.Date >= from && d.Date <= to).ToList());

    public void Add(DiaryDay day) => Days.Add(day);

    public Task<WeeklyCheckIn?> GetWeekAsync(Guid patientId, DateOnly weekStart, CancellationToken cancellationToken = default) =>
        Task.FromResult(Weeks.FirstOrDefault(w => w.PatientId == patientId && w.WeekStart == weekStart));

    public Task<WeeklyCheckIn?> GetWeekByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Weeks.FirstOrDefault(w => w.Id == id));

    public Task<IReadOnlyList<WeeklyCheckIn>> ListWeeksAsync(Guid patientId, int take, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<WeeklyCheckIn>>(Weeks.Where(w => w.PatientId == patientId).OrderByDescending(w => w.WeekStart).Take(take).ToList());

    public Task<IReadOnlyList<(WeeklyCheckIn CheckIn, string PatientName)>> ListAwaitingFeedbackAsync(
        Guid nutritionistId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<(WeeklyCheckIn, string)>>(
            Weeks.Where(w => w.NutritionistId == nutritionistId && !w.HasFeedback).Select(w => (w, "Paciente")).ToList());

    public void Add(WeeklyCheckIn checkIn) => Weeks.Add(checkIn);
}
