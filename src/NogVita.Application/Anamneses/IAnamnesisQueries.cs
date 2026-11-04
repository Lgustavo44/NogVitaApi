using NogVita.Application.Common.Pagination;
using NogVita.Domain.Anamneses;

namespace NogVita.Application.Anamneses;

public sealed record QuestionnaireSummaryResponse(Guid Id, string Title, int QuestionCount, DateTime UpdatedAt);

public sealed record QuestionnaireResponse(Guid Id, string Title, IReadOnlyList<Question> Questions, DateTime CreatedAt, DateTime? UpdatedAt);

public sealed record AnamnesisSummaryResponse(
    Guid Id,
    string Title,
    AnamnesisStatus Status,
    Guid PatientId,
    string PatientName,
    Guid NutritionistId,
    string NutritionistName,
    DateTime SentAt,
    DateTime? SubmittedAt);

// Answers vem null enquanto a anamnese está pendente.
public sealed record AnamnesisResponse(
    Guid Id,
    string Title,
    AnamnesisStatus Status,
    Guid PatientId,
    string PatientName,
    Guid NutritionistId,
    string NutritionistName,
    DateTime SentAt,
    DateTime? SubmittedAt,
    IReadOnlyList<Question> Questions,
    IReadOnlyList<Answer>? Answers);

public sealed record AnamnesisListRequest : PageRequest;

public sealed class AnamnesisListRequestValidator : PageRequestValidator<AnamnesisListRequest>;

public interface IAnamnesisQueries
{
    Task<PagedResponse<QuestionnaireSummaryResponse>> ListQuestionnairesAsync(
        Guid nutritionistId, AnamnesisListRequest request, CancellationToken cancellationToken = default);

    Task<QuestionnaireResponse?> GetQuestionnaireAsync(
        Guid nutritionistId, Guid questionnaireId, CancellationToken cancellationToken = default);

    Task<PagedResponse<AnamnesisSummaryResponse>> ListByNutritionistAndPatientAsync(
        Guid nutritionistId, Guid patientId, AnamnesisListRequest request, CancellationToken cancellationToken = default);

    Task<PagedResponse<AnamnesisSummaryResponse>> ListByPatientAsync(
        Guid patientId, AnamnesisListRequest request, CancellationToken cancellationToken = default);

    Task<AnamnesisResponse?> GetByNutritionistAsync(
        Guid nutritionistId, Guid anamnesisId, CancellationToken cancellationToken = default);

    Task<AnamnesisResponse?> GetByPatientAsync(
        Guid patientId, Guid anamnesisId, CancellationToken cancellationToken = default);
}
