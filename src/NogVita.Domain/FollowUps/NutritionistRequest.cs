using NogVita.Domain.Common;

namespace NogVita.Domain.FollowUps;

public class NutritionistRequest : Entity
{
    public const int MessageMaxLength = 500;

    public Guid PatientId { get; private set; }
    public Guid NutritionistId { get; private set; }
    public NutritionistRequestStatus Status { get; private set; }
    public string? Message { get; private set; }
    public DateTime? RespondedAtUtc { get; private set; }

    public bool IsPending => Status == NutritionistRequestStatus.Pending;

    private NutritionistRequest() { } // EF Core

    public NutritionistRequest(Guid patientId, Guid nutritionistId, string? message)
    {
        if (patientId == nutritionistId)
        {
            throw new DomainException("Você não pode solicitar acompanhamento a si mesmo.");
        }

        var normalizedMessage = string.IsNullOrWhiteSpace(message) ? null : message.Trim();

        if (normalizedMessage is { Length: > MessageMaxLength })
        {
            throw new DomainException($"A mensagem pode ter no máximo {MessageMaxLength} caracteres.");
        }

        PatientId = patientId;
        NutritionistId = nutritionistId;
        Message = normalizedMessage;
        Status = NutritionistRequestStatus.Pending;
    }

    public CareRelationship Accept(DateTime nowUtc)
    {
        EnsurePending();
        Status = NutritionistRequestStatus.Accepted;
        RespondedAtUtc = nowUtc;
        return new CareRelationship(PatientId, NutritionistId, nowUtc);
    }

    public void Reject(DateTime nowUtc)
    {
        EnsurePending();
        Status = NutritionistRequestStatus.Rejected;
        RespondedAtUtc = nowUtc;
    }

    public void Cancel()
    {
        EnsurePending();
        Status = NutritionistRequestStatus.Cancelled;
    }

    private void EnsurePending()
    {
        if (!IsPending)
        {
            throw new DomainException("Esta solicitação não está mais pendente.");
        }
    }
}