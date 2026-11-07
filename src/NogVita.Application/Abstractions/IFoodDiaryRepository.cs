using NogVita.Domain.FoodDiary;

namespace NogVita.Application.Abstractions;

public interface IFoodDiaryRepository
{
    Task<DiaryDay?> GetDayAsync(Guid patientId, DateOnly date, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DiaryDay>> ListDaysAsync(Guid patientId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default);

    void Add(DiaryDay day);

    // Os pacientes que o nutricionista acompanha agora (ativos), cada um com os dias do diário no período.
    Task<IReadOnlyList<(Guid PatientId, string PatientName, IReadOnlyList<DiaryDay> Days)>> ListFollowedPatientsDiaryAsync(
        Guid nutritionistId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default);

    Task<WeeklyCheckIn?> GetWeekAsync(Guid patientId, DateOnly weekStart, CancellationToken cancellationToken = default);

    Task<WeeklyCheckIn?> GetWeekByIdAsync(Guid id, CancellationToken cancellationToken = default);

    // As semanas do paciente, da mais recente para a mais antiga.
    Task<IReadOnlyList<WeeklyCheckIn>> ListWeeksAsync(Guid patientId, int take, CancellationToken cancellationToken = default);

    // Os check-ins dos pacientes que o nutricionista acompanha agora e que ainda esperam a devolutiva, com o nome do paciente.
    Task<IReadOnlyList<(WeeklyCheckIn CheckIn, string PatientName)>> ListAwaitingFeedbackAsync(
        Guid nutritionistId, CancellationToken cancellationToken = default);

    void Add(WeeklyCheckIn checkIn);
}
