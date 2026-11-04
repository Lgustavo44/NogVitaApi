using NogVita.Domain.Documents;

namespace NogVita.Application.Abstractions;

public interface IIssuedDocumentRepository
{
    Task<IssuedDocument?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    void Add(IssuedDocument document);
}
