using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NogVita.Domain.Foods;

namespace NogVita.Infrastructure.Persistence.Configurations;

public class FoodConfiguration : IEntityTypeConfiguration<Food>
{
    public void Configure(EntityTypeBuilder<Food> builder)
    {
        builder.ToTable("tb_food");

        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).ValueGeneratedNever();

        builder.Property(f => f.Source)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(f => f.SourceReference)
            .HasMaxLength(Food.SourceReferenceMaxLength)
            .IsRequired();

        builder.Property(f => f.Name)
            .HasMaxLength(Food.NameMaxLength)
            .IsRequired();

        builder.Property(f => f.Brand)
            .HasMaxLength(Food.BrandMaxLength)
            .IsRequired(false);

        builder.Property(f => f.Barcode)
            .HasMaxLength(14)
            .IsRequired(false);

        builder.Property(f => f.Category)
            .HasMaxLength(Food.CategoryMaxLength)
            .IsRequired(false);

        builder.Property(f => f.IsActive)
            .IsRequired();

        builder.ComplexProperty(f => f.Nutrients, nutrients =>
        {
            nutrients.Ignore(n => n.IsComplete);

            nutrients.Property(n => n.EnergyKcal).HasPrecision(9, 4);
            nutrients.Property(n => n.Protein).HasPrecision(9, 4);
            nutrients.Property(n => n.Carbohydrate).HasPrecision(9, 4);
            nutrients.Property(n => n.Fat).HasPrecision(9, 4);
            nutrients.Property(n => n.Fiber).HasPrecision(9, 4);
            nutrients.Property(n => n.SodiumMg).HasPrecision(9, 4);
        });

        builder.HasIndex(f => new { f.Source, f.SourceReference }, "ux_food_source_reference")
            .HasDatabaseName("ux_food_source_reference")
            .IsUnique();

        builder.HasIndex(f => f.Barcode, "ux_food_barcode")
            .HasDatabaseName("ux_food_barcode")
            .IsUnique()
            .HasFilter("barcode IS NOT NULL");
    }
}