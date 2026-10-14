using NogVita.Domain.Common;

namespace NogVita.Domain.NutritionPlans;

public class NutritionPlan : Entity
{
    public const int NameMaxLength = 100;
    public const int DescriptionMaxLength = 1000;

    public Guid PatientId { get; private set; }
    public Guid NutritionistId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly? EndDate { get; private set; }
    public NutritionPlanStatus Status { get; private set; }

    public bool IsEditable => Status is NutritionPlanStatus.Draft or NutritionPlanStatus.Active;

    private NutritionPlan() { } // EF Core

    public NutritionPlan(
        Guid patientId,
        Guid nutritionistId,
        string name,
        string? description,
        DateOnly startDate,
        DateOnly? endDate)
    {
        if (patientId == nutritionistId)
        {
            throw new DomainException("O nutricionista não pode criar um plano para si mesmo.");
        }

        PatientId = patientId;
        NutritionistId = nutritionistId;
        Status = NutritionPlanStatus.Draft;
        SetDetails(name, description, startDate, endDate);
    }

    public void UpdateDetails(string name, string? description, DateOnly startDate, DateOnly? endDate)
    {
        if (!IsEditable)
        {
            throw new DomainException("Um plano finalizado ou cancelado não pode ser alterado.");
        }

        SetDetails(name, description, startDate, endDate);
    }

    public void Activate()
    {
        if (Status != NutritionPlanStatus.Draft)
        {
            throw new DomainException("Só um plano em rascunho pode ser ativado.");
        }

        Status = NutritionPlanStatus.Active;
    }

    public void Cancel()
    {
        if (Status != NutritionPlanStatus.Draft)
        {
            throw new DomainException("Só um plano em rascunho pode ser cancelado.");
        }

        Status = NutritionPlanStatus.Cancelled;
    }

    public void Finish(DateOnly today)
    {
        if (Status != NutritionPlanStatus.Active)
        {
            throw new DomainException("Só um plano ativo pode ser finalizado.");
        }

        var endDate = EndDate is null || EndDate > today ? today : EndDate.Value;
        EndDate = endDate < StartDate ? StartDate : endDate;
        Status = NutritionPlanStatus.Finished;
    }

    private void SetDetails(string name, string? description, DateOnly startDate, DateOnly? endDate)
    {
        var normalizedName = name?.Trim();

        if (string.IsNullOrEmpty(normalizedName))
        {
            throw new DomainException("O nome do plano é obrigatório.");
        }

        if (normalizedName.Length > NameMaxLength)
        {
            throw new DomainException($"O nome do plano pode ter no máximo {NameMaxLength} caracteres.");
        }

        var normalizedDescription = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

        if (normalizedDescription is { Length: > DescriptionMaxLength })
        {
            throw new DomainException($"A descrição pode ter no máximo {DescriptionMaxLength} caracteres.");
        }


        if (endDate.HasValue && endDate < startDate)
        {
            throw new DomainException("A data de fim não pode ser anterior à data de início.");
        }

        Name = normalizedName;
        Description = normalizedDescription;
        StartDate = startDate;
        EndDate = endDate;
    }
}