using NogVita.Application.Abstractions;
using NogVita.Domain.Anamneses;

namespace NogVita.UnitTests.Fakes;

public sealed class FakeAnamnesisRepository : IAnamnesisRepository
{
    public List<Anamnesis> Anamneses { get; } = [];

    public Task<Anamnesis?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Anamneses.FirstOrDefault(a => a.Id == id));

    public void Add(Anamnesis anamnesis) => Anamneses.Add(anamnesis);
}
