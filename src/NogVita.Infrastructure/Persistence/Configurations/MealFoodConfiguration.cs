using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NogVita.Domain.Foods;
using NogVita.Domain.NutritionPlans;

namespace NogVita.Infrastructure.Persistence.Configurations;

public class MealFoodConfiguration : IEntityTypeConfiguration<MealFood>
{
    public void Configure(EntityTypeBuilder<MealFood> builder)
    {
        builder.ToTable("tb_meal_food");

        builder.HasKey(mf => mf.Id);
        builder.Property(mf => mf.Id).ValueGeneratedNever();

        builder.Property(mf => mf.QuantityInGrams)
            .HasPrecision(7, 2)
            .IsRequired();

        // Cópia do alimento no momento em que entrou no plano: os mesmos limites e a mesma precisão da tb_food.
        builder.ComplexProperty(mf => mf.Snapshot, snapshot =>
        {
            snapshot.Property(s => s.Name)
                .HasMaxLength(Food.NameMaxLength)
                .IsRequired();

            snapshot.Property(s => s.Brand)
                .HasMaxLength(Food.BrandMaxLength)
                .IsRequired(false);

            snapshot.Property(s => s.Source)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            snapshot.Property(s => s.SourceReference)
                .HasMaxLength(Food.SourceReferenceMaxLength)
                .IsRequired();

            snapshot.Property(s => s.TakenAtUtc)
                .IsRequired();

            snapshot.ComplexProperty(s => s.Nutrients, NutrientsMapping.Configure);
        });

        // O alimento não é apagado (só desativado); Restrict impede que um apagamento manual quebre os planos.
        builder.HasOne<Food>()
            .WithMany()
            .HasForeignKey(mf => mf.FoodId)
            .OnDelete(DeleteBehavior.Restrict);

        // D141: o mesmo alimento entra uma vez só em cada refeição.
        builder.HasIndex(mf => new { mf.MealId, mf.FoodId }, "ux_meal_food_meal_food")
            .HasDatabaseName("ux_meal_food_meal_food")
            .IsUnique();

        builder.HasIndex(mf => mf.FoodId, "ix_meal_food_food_id")
            .HasDatabaseName("ix_meal_food_food_id");
    }
}
