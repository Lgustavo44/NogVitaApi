using NogVita.Domain.Common;

namespace NogVita.Domain.Users;

public class NutritionistProfile : Entity
{
    private const int MaxCrnRegion = 11;

    public Guid UserId { get; private set; }
    public int CrnRegion { get; private set; }
    public string CrnNumber { get; private set; } = null!;
    public bool IsActive { get; private set; }

    private NutritionistProfile() { } // EF Core

    internal NutritionistProfile(Guid userId, int crnRegion, string crnNumber)
    {
        if (crnRegion < 1 || crnRegion > MaxCrnRegion)
            throw new DomainException("Região do CRN inválida.");
        if (string.IsNullOrWhiteSpace(crnNumber))
            throw new DomainException("O número do CRN é obrigatório.");
        CrnNumber = crnNumber.Trim();
        UserId = userId;
        CrnRegion = crnRegion;
        IsActive = false;
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}