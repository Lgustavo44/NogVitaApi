using NogVita.Domain.Common;

namespace NogVita.Domain.Audit;

// Trilha de auditoria das ações do admin que não deixam rastro nos próprios dados (por exemplo, um cadastro apagado).
// Guarda só Ids e um resumo: nunca o CPF nem o e-mail de quem teve os dados apagados.
public class AdminAuditEntry : Entity
{
    public const int DetailsMaxLength = 200;

    public Guid AdminUserId { get; private set; }
    public AdminAuditAction Action { get; private set; }

    // Sem FK: o usuário afetado pode ter sido apagado.
    public Guid TargetUserId { get; private set; }

    public string? Details { get; private set; }
    public DateTime OccurredAtUtc { get; private set; }

    private AdminAuditEntry() { } // EF Core

    public AdminAuditEntry(Guid adminUserId, AdminAuditAction action, Guid targetUserId, string? details, DateTime occurredAtUtc)
    {
        var normalizedDetails = string.IsNullOrWhiteSpace(details) ? null : details.Trim();

        if (normalizedDetails is { Length: > DetailsMaxLength })
        {
            throw new DomainException($"O resumo da auditoria pode ter no máximo {DetailsMaxLength} caracteres.");
        }

        AdminUserId = adminUserId;
        Action = action;
        TargetUserId = targetUserId;
        Details = normalizedDetails;
        OccurredAtUtc = occurredAtUtc;
    }
}
