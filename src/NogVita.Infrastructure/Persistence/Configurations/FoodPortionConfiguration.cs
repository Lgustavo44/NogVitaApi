using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NogVita.Domain.Foods;

namespace NogVita.Infrastructure.Persistence.Configurations;

public class FoodPortionConfiguration : IEntityTypeConfiguration<FoodPortion>
{
    public void Configure(EntityTypeBuilder<FoodPortion> builder)
    {
        builder.ToTable("tb_food_portion");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.Name).HasMaxLength(FoodPortion.NameMaxLength).IsRequired();
        builder.Property(p => p.Grams).HasPrecision(7, 2).IsRequired();
        builder.Property(p => p.Source).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.HasIndex(p => new { p.FoodId, p.Name })
            .IsUnique()
            .HasDatabaseName("ux_food_portion_food_name");
    }
}
