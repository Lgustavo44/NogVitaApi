using Microsoft.EntityFrameworkCore;
using NogVita.Application.Abstractions;
using NogVita.Domain.Audit;
using NogVita.Domain.Auth;
using NogVita.Domain.Common;
using NogVita.Domain.FollowUps;
using NogVita.Domain.Foods;
using NogVita.Domain.Invitations;
using NogVita.Domain.NutritionPlans;
using NogVita.Domain.Users;
using NogVita.Domain.WeightRecords;

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
    public DbSet<WeightRecord> WeightRecords => Set<WeightRecord>();
    public DbSet<AdminAuditEntry> AdminAuditEntries => Set<AdminAuditEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("unaccent");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NogVitaDbContext).Assembly);
    }

    // D171: a nova tentativa da conexão (EnableRetryOnFailure) está desligada, e esta estratégia executa uma vez só.
    // Se um dia for ligada, a operação passa a poder rodar duas vezes com as entidades já alteradas na memória
    // (na ativação de um plano, o Finish() do plano anterior lançaria uma DomainException). Antes de ligar,
    // a operação precisa recarregar o que lê (por exemplo, com um ChangeTracker.Clear() no começo de cada tentativa).
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

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<Entity>())
        {
            if (entry.State == EntityState.Added)
                entry.Property(e => e.CreatedAt).CurrentValue = now;
            else if (entry.State == EntityState.Modified)
                entry.Property(e => e.UpdatedAt).CurrentValue = now;
        }

        try
        {
            return await base.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (UniqueViolationTranslator.TryTranslate(exception, out var conflict))
        {
            // Um índice único barrou a gravação (o clique duplo, ou duas abas ao mesmo tempo): 409, e não 500.
            throw conflict!;
        }
    }
}