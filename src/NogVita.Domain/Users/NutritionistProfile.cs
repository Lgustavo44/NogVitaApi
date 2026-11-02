using NogVita.Domain.Common;

namespace NogVita.Domain.Users;

public class NutritionistProfile : Entity
{
    private const int MaxCrnRegion = 11;

    public Guid UserId { get; private set; }
    public int CrnRegion { get; private set; }
    public string CrnNumber { get; private set; } = null!;
    public bool IsActive { get; private set; }
    public const int PhotoPathMaxLength = 200;
    public string? PhotoPath { get; private set; }
    public const int BioMaxLength = 500;
    public string? Bio { get; private set; }

    private NutritionistProfile() { } // EF Core

    internal NutritionistProfile(Guid userId, int crnRegion, string crnNumber)
    {
        if (crnRegion < 1 || crnRegion > MaxCrnRegion)
        {
            throw new DomainException("Região do CRN inválida.");
        }

        if (string.IsNullOrWhiteSpace(crnNumber))
        {
            throw new DomainException("O número do CRN é obrigatório.");
        }

        CrnNumber = crnNumber.Trim();
        UserId = userId;
        CrnRegion = crnRegion;
        IsActive = false;
    }

    internal void UpdateBio(string? bio)
    {
        var normalizedBio = string.IsNullOrWhiteSpace(bio) ? null : bio.Trim();

        if (normalizedBio is { Length: > BioMaxLength })
        {
            throw new DomainException($"A apresentação pode ter no máximo {BioMaxLength} caracteres.");
        }

        Bio = normalizedBio;
    }

    // Devolve o caminho anterior: o arquivo antigo só é apagado depois que o banco confirma a troca.
    internal string? ChangePhoto(string? photoPath)
    {
        // O caminho é gerado pela API (D-V2-10): um caminho inválido aqui é bug, e não erro do usuário.
        if (photoPath is not null && (string.IsNullOrWhiteSpace(photoPath) || photoPath.Length > PhotoPathMaxLength))
        {
            throw new ArgumentException("Caminho da foto inválido.", nameof(photoPath));
        }

        var previousPath = PhotoPath;
        PhotoPath = photoPath;
        return previousPath;
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
