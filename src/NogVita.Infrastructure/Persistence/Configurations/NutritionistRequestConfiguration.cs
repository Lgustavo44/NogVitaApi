using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NogVita.Domain.FollowUps;
using NogVita.Domain.Users;

namespace NogVita.Infrastructure.Persistence.Configurations;

public class NutritionistRequestConfiguration : IEntityTypeConfiguration<NutritionistRequest>
{
    public void Configure(EntityTypeBuilder<NutritionistRequest> builder)
    {
        builder.ToTable("tb_nutritionist_request");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();

        builder.Property(r => r.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(r => r.Message)
            .HasMaxLength(NutritionistRequest.MessageMaxLength)
            .IsRequired(false);

        builder.Property(r => r.RespondedAtUtc).IsRequired(false);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(r => r.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(r => r.NutritionistId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(r => r.PatientId, "ux_nutritionist_request_patient_pending")
            .HasDatabaseName("ux_nutritionist_request_patient_pending")
            .IsUnique()
            .HasFilter("status = 'Pending'");

        builder.HasIndex(r => r.PatientId, "ix_nutritionist_request_patient_id")
            .HasDatabaseName("ix_nutritionist_request_patient_id");

        builder.HasIndex(r => new { r.NutritionistId, r.Status }, "ix_nutritionist_request_nutritionist_status")
            .HasDatabaseName("ix_nutritionist_request_nutritionist_status");
    }
}