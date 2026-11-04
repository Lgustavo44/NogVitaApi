using Microsoft.EntityFrameworkCore;
using NogVita.Application.Abstractions;
using NogVita.Domain.Anamneses;

namespace NogVita.Infrastructure.Persistence.Repositories;

internal sealed class AnamnesisRepository(NogVitaDbContext context) : IAnamnesisRepository
{
    public Task<Anamnesis?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Anamneses.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public void Add(Anamnesis anamnesis) => context.Anamneses.Add(anamnesis);
}
