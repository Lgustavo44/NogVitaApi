using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NogVita.Domain.ProgressPhotos;
using NogVita.Domain.Users;

namespace NogVita.Infrastructure.Persistence.Configurations;

public class ProgressPhotoConfiguration : IEntityTypeConfiguration<ProgressPhoto>
{
    public void Configure(EntityTypeBuilder<ProgressPhoto> builder)
    {
        builder.ToTable("tb_progress_photo");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.TakenOn).IsRequired();

        builder.Property(p => p.Angle)
            .HasConversion<string>()
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(p => p.StoragePath)
            .HasMaxLength(ProgressPhoto.StoragePathMaxLength)
            .IsRequired();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(p => p.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        // D-V2-05: uma foto por ângulo por data. Duas abas enviando ao mesmo tempo: a segunda recebe 409.
        builder.HasIndex(p => new { p.PatientId, p.TakenOn, p.Angle })
            .IsUnique()
            .HasDatabaseName("ux_progress_photo_patient_day_angle");
    }
}
