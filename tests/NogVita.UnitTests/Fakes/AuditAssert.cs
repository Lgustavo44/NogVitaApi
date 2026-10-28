using NogVita.Application.Admin;
using NogVita.Domain.Audit;

namespace NogVita.UnitTests.Fakes;

// D173: a auditoria registra só o que aconteceu. Os testes conferem a linha certa, e também que nada aparece numa recusa.
public static class AuditAssert
{
    public static CapturingLogger<AdminAuditLog>.Entry Recorded(
        CapturingLogger<AdminAuditLog> logger, Guid adminId, AdminAuditAction action, Guid targetId)
    {
        var entry = Assert.Single(logger.Entries, e => e.EventId == AdminAuditLog.EventId);
        Assert.Equal(adminId, entry.Values["AdminId"]);
        Assert.Equal(action, entry.Values["AdminAction"]);
        Assert.Equal(targetId, entry.Values["TargetId"]);
        return entry;
    }

    public static void Nothing(CapturingLogger<AdminAuditLog> logger) =>
        Assert.DoesNotContain(logger.Entries, e => e.EventId == AdminAuditLog.EventId);
}
