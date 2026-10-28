using NogVita.Application.Abstractions;
using NogVita.Domain.Audit;

namespace NogVita.Infrastructure.Persistence.Repositories;

internal sealed class AdminAuditRepository(NogVitaDbContext context) : IAdminAuditRepository
{
    public void Add(AdminAuditEntry entry) => context.AdminAuditEntries.Add(entry);
}
