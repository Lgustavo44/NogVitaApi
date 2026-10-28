using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NogVita.Domain.Audit;
using NogVita.Domain.Users;

namespace NogVita.Infrastructure.Persistence.Configurations;

public class AdminAuditEntryConfiguration : IEntityTypeConfiguration<AdminAuditEntry>
{
    public void Configure(EntityTypeBuilder<AdminAuditEntry> builder)
    {
        builder.ToTable("tb_admin_audit_entry");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();

        builder.Property(a => a.Action)
            .HasConversion<string>()
            .HasMaxLength(60)
            .IsRequired();

        builder.Property(a => a.Details)
            .HasMaxLength(AdminAuditEntry.DetailsMaxLength)
            .IsRequired(false);

        builder.Property(a => a.OccurredAtUtc).IsRequired();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(a => a.AdminUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => a.TargetUserId);
    }
}
