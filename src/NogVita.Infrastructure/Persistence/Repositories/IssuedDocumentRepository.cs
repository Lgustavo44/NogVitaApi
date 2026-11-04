using Microsoft.EntityFrameworkCore;
using NogVita.Application.Abstractions;
using NogVita.Domain.Documents;

namespace NogVita.Infrastructure.Persistence.Repositories;

internal sealed class IssuedDocumentRepository(NogVitaDbContext context) : IIssuedDocumentRepository
{
    public Task<IssuedDocument?> GetByCodeAsync(string code, CancellationToken cancellationToken = default) =>
        context.IssuedDocuments.AsNoTracking().FirstOrDefaultAsync(d => d.Code == code, cancellationToken);

    public void Add(IssuedDocument document) => context.IssuedDocuments.Add(document);
}
