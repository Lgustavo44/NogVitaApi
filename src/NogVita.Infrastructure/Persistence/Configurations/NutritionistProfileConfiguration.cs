using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NogVita.Domain.Users;

namespace NogVita.Infrastructure.Persistence.Configurations;

public class NutritionistProfileConfiguration : IEntityTypeConfiguration<NutritionistProfile>
{
    public void Configure(EntityTypeBuilder<NutritionistProfile> builder)
    {
        builder.ToTable("tb_nutritionist_profile");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.CrnRegion)
            .IsRequired();

        builder.Property(p => p.CrnNumber)
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(p => 
        new { p.CrnRegion, p.CrnNumber })
        .IsUnique();
    }
}