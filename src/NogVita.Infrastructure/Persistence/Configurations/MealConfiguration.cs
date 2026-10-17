using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NogVita.Domain.NutritionPlans;

namespace NogVita.Infrastructure.Persistence.Configurations;

public class MealConfiguration : IEntityTypeConfiguration<Meal>
{
    public void Configure(EntityTypeBuilder<Meal> builder)
    {
        builder.ToTable("tb_meal");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedNever();

        builder.Property(m => m.Name)
            .HasMaxLength(Meal.NameMaxLength)
            .IsRequired();

        builder.Property(m => m.Notes)
            .HasMaxLength(Meal.NotesMaxLength)
            .IsRequired(false);

        builder.Property(m => m.Order)
            .HasColumnName("meal_order")
            .IsRequired();

        builder.HasIndex(m => m.NutritionPlanId, "ix_meal_nutrition_plan_id")
            .HasDatabaseName("ix_meal_nutrition_plan_id");

        builder.HasMany(m => m.Foods)
            .WithOne()
            .HasForeignKey(mf => mf.MealId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(m => m.Foods)
            .HasField("_foods")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}