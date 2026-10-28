using NogVita.Domain.Audit;

namespace NogVita.Application.Abstractions;

public interface IAdminAuditRepository
{
    void Add(AdminAuditEntry entry);
}
