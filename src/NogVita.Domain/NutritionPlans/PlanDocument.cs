using NogVita.Domain.Common;

namespace NogVita.Domain.NutritionPlans;

// Res. CFN 760/2023, art. 10: o documento identifica o nutricionista, tem data e um código verificador.
// Um registro por PDF gerado. Guarda o nutricionista como era na emissão, e nada do paciente: a consulta pelo código é pública.
public class PlanDocument : Entity
{
    public const int CodeLength = 12;
    public const int Sha256Length = 64;

    public string Code { get; private set; } = null!;
    public Guid PlanId { get; private set; }
    public DateTime IssuedAtUtc { get; private set; }
    public string NutritionistName { get; private set; } = null!;
    public int CrnRegion { get; private set; }
    public string CrnNumber { get; private set; } = null!;
    public string Sha256 { get; private set; } = null!;

    private PlanDocument() { } // EF Core

    public PlanDocument(string code, Guid planId, DateTime issuedAtUtc, string nutritionistName, int crnRegion, string crnNumber, string sha256)
    {
        if (code is not { Length: CodeLength })
        {
            throw new ArgumentException("Código verificador inválido.", nameof(code));
        }

        if (sha256 is not { Length: Sha256Length })
        {
            throw new ArgumentException("Hash do documento inválido.", nameof(sha256));
        }

        Code = code;
        PlanId = planId;
        IssuedAtUtc = issuedAtUtc;
        NutritionistName = nutritionistName;
        CrnRegion = crnRegion;
        CrnNumber = crnNumber;
        Sha256 = sha256;
    }
}
