using NogVita.Domain.Anamneses;

namespace NogVita.Application.Abstractions;

public interface IAnamnesisRepository
{
    Task<Anamnesis?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    void Add(Anamnesis anamnesis);
}
