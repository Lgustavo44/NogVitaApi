using NogVita.Application.Abstractions;
using NogVita.Domain.Audit;

namespace NogVita.UnitTests.Fakes;

public sealed class FakeAdminAuditRepository : IAdminAuditRepository
{
    public List<AdminAuditEntry> Entries { get; } = [];

    public void Add(AdminAuditEntry entry) => Entries.Add(entry);
}
