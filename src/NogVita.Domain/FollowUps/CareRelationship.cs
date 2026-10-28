using NogVita.Domain.Common;

namespace NogVita.Domain.FollowUps;

public class CareRelationship : Entity
{
    public Guid PatientId { get; private set; }
    public Guid NutritionistId { get; private set; }
    public DateTime StartedAtUtc { get; private set; }
    public DateTime? EndedAtUtc { get; private set; }
    public Guid? EndedByUserId { get; private set; }

    public bool IsActive => EndedAtUtc is null;

    private CareRelationship() { } // EF Core

    internal CareRelationship(Guid patientId, Guid nutritionistId, DateTime startedAtUtc)
    {
        PatientId = patientId;
        NutritionistId = nutritionistId;
        StartedAtUtc = startedAtUtc;
    }

    public void End(Guid endedByUserId, DateTime nowUtc)
    {
        if (endedByUserId != PatientId && endedByUserId != NutritionistId)
        {
            throw new DomainException("Só o paciente ou o nutricionista podem encerrar o acompanhamento.");
        }

        Close(endedByUserId, nowUtc);
    }

    // Quando o admin desativa o nutricionista, o acompanhamento é encerrado em nome do admin.
    public void EndByAdmin(Guid adminId, DateTime nowUtc) => Close(adminId, nowUtc);

    private void Close(Guid endedByUserId, DateTime nowUtc)
    {
        if (!IsActive)
        {
            throw new DomainException("Este acompanhamento já foi encerrado.");
        }

        EndedAtUtc = nowUtc;
        EndedByUserId = endedByUserId;
    }
}