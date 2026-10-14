using Microsoft.EntityFrameworkCore;
using NogVita.Application.Abstractions;
using NogVita.Domain.Auth;
using NogVita.Domain.Common;
using NogVita.Domain.FollowUps;
using NogVita.Domain.Foods;
using NogVita.Domain.Invitations;
using NogVita.Domain.NutritionPlans;
using NogVita.Domain.Users;

namespace NogVita.Infrastructure.Persistence;

public class NogVitaDbContext(DbContextOptions<NogVitaDbContext> options) : DbContext(options), IUnitOfWork
{
    public DbSet<User> Users => Set<User>();
    public DbSet<PatientProfile> PatientProfiles => Set<PatientProfile>();
    public DbSet<NutritionistProfile> NutritionistProfiles => Set<NutritionistProfile>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<NutritionistInvitation> NutritionistInvitations => Set<NutritionistInvitation>();
    public DbSet<EmailConfirmationToken> EmailConfirmationTokens => Set<EmailConfirmationToken>();
    public DbSet<NutritionistRequest> NutritionistRequests => Set<NutritionistRequest>();
    public DbSet<CareRelationship> CareRelationships => Set<CareRelationship>();
    public DbSet<Food> Foods => Set<Food>();
    public DbSet<NutritionPlan> NutritionPlans => Set<NutritionPlan>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("unaccent");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NogVitaDbContext).Assembly);
    }

    public Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default)
    {
        var strategy = Database.CreateExecutionStrategy();

        return strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await Database.BeginTransactionAsync(cancellationToken);
            await operation(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        });
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