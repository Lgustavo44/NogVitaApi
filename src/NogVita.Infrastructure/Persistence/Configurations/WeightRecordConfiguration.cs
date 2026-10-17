using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NogVita.Domain.Users;
using NogVita.Domain.WeightRecords;

namespace NogVita.Infrastructure.Persistence.Configurations;

public class WeightRecordConfiguration : IEntityTypeConfiguration<WeightRecord>
{
    public void Configure(EntityTypeBuilder<WeightRecord> builder)
    {
        builder.ToTable("tb_weight_record");

        builder.HasKey(w => w.Id);
        builder.Property(w => w.Id).ValueGeneratedNever();

        builder.Property(w => w.WeightKg)
            .HasPrecision(5, 2)
            .IsRequired();

        builder.Property(w => w.RecordedOn).IsRequired();

        builder.Property(w => w.Notes)
            .HasMaxLength(WeightRecord.NotesMaxLength)
            .IsRequired(false);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(w => w.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(w => w.RecordedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(w => new { w.PatientId, w.RecordedOn })
            .IsUnique()
            .HasDatabaseName("ux_weight_record_patient_day");
    }
}