using NogVita.Application.Abstractions;
using NogVita.Domain.Documents;

namespace NogVita.UnitTests.Fakes;

public sealed class FakeIssuedDocumentRepository : IIssuedDocumentRepository
{
    public List<IssuedDocument> Documents { get; } = [];

    public Task<IssuedDocument?> GetByCodeAsync(string code, CancellationToken cancellationToken = default) =>
        Task.FromResult(Documents.FirstOrDefault(d => d.Code == code));

    public void Add(IssuedDocument document) => Documents.Add(document);
}
