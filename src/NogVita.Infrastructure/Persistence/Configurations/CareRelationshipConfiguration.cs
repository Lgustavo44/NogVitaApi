using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NogVita.Domain.FollowUps;
using NogVita.Domain.Users;

namespace NogVita.Infrastructure.Persistence.Configurations;

public class CareRelationshipConfiguration : IEntityTypeConfiguration<CareRelationship>
{
    public void Configure(EntityTypeBuilder<CareRelationship> builder)
    {
        builder.ToTable("tb_care_relationship");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.StartedAtUtc).IsRequired();
        builder.Property(c => c.EndedAtUtc).IsRequired(false);
        builder.Property(c => c.EndedByUserId).IsRequired(false);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(r => r.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(r => r.NutritionistId)
            .OnDelete(DeleteBehavior.Restrict);


        builder.HasIndex(c => c.PatientId, "ux_care_relationship_patient_active")
            .IsUnique()
            .HasDatabaseName("ux_care_relationship_patient_active")
            .HasFilter("ended_at_utc IS NULL");


        builder.HasIndex(c => new { c.NutritionistId, c.EndedAtUtc })
            .HasDatabaseName("ix_care_relationship_nutritionist_ended");

    }
}