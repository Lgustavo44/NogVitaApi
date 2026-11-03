using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NogVita.Domain.Users;

namespace NogVita.Infrastructure.Persistence.Configurations;

public class PatientProfileConfiguration : IEntityTypeConfiguration<PatientProfile>
{
    public void Configure(EntityTypeBuilder<PatientProfile> builder)
    {
        builder.ToTable("tb_patient_profile");

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).ValueGeneratedNever();

        builder.Property(u => u.BirthDate)
            .IsRequired();

        builder.Property(u => u.HeightInCm);

        builder.Property(n => n.PhotoPath)
            .HasMaxLength(PatientProfile.PhotoPathMaxLength);

        builder.Property(p => p.BiologicalSex)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(p => p.ProgressPhotosConsentVersion)
            .HasMaxLength(PatientProfile.ConsentVersionMaxLength);

        builder.Property(p => p.HealthDataConsentVersion)
            .HasMaxLength(PatientProfile.ConsentVersionMaxLength);

        builder.Property(p => p.Goal)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
    }
}
