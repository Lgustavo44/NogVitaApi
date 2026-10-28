using Microsoft.Extensions.Logging;
using NogVita.Application.Admin;
using NogVita.Domain.Audit;
using NogVita.UnitTests.Fakes;

namespace NogVita.UnitTests.Application.Admin;

public class AdminAuditLogTests
{
    [Fact]
    public void Should_Use_Its_Own_Event_Id_And_Information_Level()
    {
        var logger = new CapturingLogger<AdminAuditLog>();

        new AdminAuditLog(logger).Record(Guid.NewGuid(), AdminAuditAction.UserDeactivated, Guid.NewGuid());

        var entry = Assert.Single(logger.Entries);
        Assert.Equal(9100, entry.EventId.Id);
        Assert.Equal("AdminAudit", entry.EventId.Name);
        Assert.Equal(LogLevel.Information, entry.Level);
    }

    [Fact]
    public void Should_Write_The_Same_Format_For_Every_Action()
    {
        var logger = new CapturingLogger<AdminAuditLog>();
        var audit = new AdminAuditLog(logger);

        foreach (var action in Enum.GetValues<AdminAuditAction>())
        {
            audit.Record(Guid.NewGuid(), action, Guid.NewGuid());
        }

        Assert.Equal(Enum.GetValues<AdminAuditAction>().Length, logger.Entries.Count);
        Assert.All(logger.Entries, entry =>
        {
            Assert.StartsWith("Auditoria: o admin ", entry.Message);
            Assert.Equal(["AdminId", "AdminAction", "TargetId", "AuditDetails", "{OriginalFormat}"], entry.Values.Keys);
        });
    }
}
