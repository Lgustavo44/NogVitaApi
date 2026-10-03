using Microsoft.EntityFrameworkCore;
using NogVita.Domain.Common;
using NogVita.Domain.Users;

namespace NogVita.Infrastructure.Persistence;

public class NogVitaDbContext(DbContextOptions<NogVitaDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<PatientProfile> PatientProfiles => Set<PatientProfile>();
    public DbSet<NutritionistProfile> NutritionistProfiles => Set<NutritionistProfile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NogVitaDbContext).Assembly);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<Entity>())
        {
            if (entry.State == EntityState.Added)
                entry.Property(e => e.CreatedAt).CurrentValue = now;
            else if (entry.State == EntityState.Modified)
                entry.Property(e => e.UpdatedAt).CurrentValue = now;
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}