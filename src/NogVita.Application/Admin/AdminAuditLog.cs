using Microsoft.Extensions.Logging;
using NogVita.Domain.Audit;

namespace NogVita.Application.Admin;

// D173: a auditoria de todas as ações do admin, num formato único e com um EventId próprio.
// Com o EventId, dá para filtrar no log só as ações do admin, sem depender do texto da mensagem.
// Só Ids: nunca o nome, o CPF ou o e-mail de ninguém. E só é chamada depois que a ação deu certo.
public sealed class AdminAuditLog(ILogger<AdminAuditLog> logger)
{
    public static readonly EventId EventId = new(9100, "AdminAudit");

    public void Record(Guid adminId, AdminAuditAction action, Guid targetId, string? details = null)
    {
        logger.LogInformation(
            EventId,
            "Auditoria: o admin {AdminId} executou {AdminAction} em {TargetId}. {AuditDetails}",
            adminId, action, targetId, details ?? "-");
    }
}
