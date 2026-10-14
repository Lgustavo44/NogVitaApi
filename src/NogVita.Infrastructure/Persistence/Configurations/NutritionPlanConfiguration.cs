using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NogVita.Domain.NutritionPlans;
using NogVita.Domain.Users;

namespace NogVita.Infrastructure.Persistence.Configurations;

public class NutritionPlanConfiguration : IEntityTypeConfiguration<NutritionPlan>
{
    public void Configure(EntityTypeBuilder<NutritionPlan> builder)
    {
        builder.ToTable("tb_nutrition_plan");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.Name)
            .HasMaxLength(NutritionPlan.NameMaxLength)
            .IsRequired();

        builder.Property(p => p.Description)
            .HasMaxLength(NutritionPlan.DescriptionMaxLength)
            .IsRequired(false);

        builder.Property(p => p.StartDate).IsRequired();
        builder.Property(p => p.EndDate).IsRequired(false);

        builder.Property(p => p.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(p => p.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(p => p.NutritionistId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => p.PatientId, "ux_nutrition_plan_patient_active")
            .HasDatabaseName("ux_nutrition_plan_patient_active")
            .IsUnique()
            .HasFilter("status = 'Active'");

        builder.HasIndex(p => new { p.PatientId, p.Status }, "ix_nutrition_plan_patient_status")
            .HasDatabaseName("ix_nutrition_plan_patient_status");

        builder.HasIndex(p => new { p.NutritionistId, p.PatientId }, "ix_nutrition_plan_nutritionist_patient")
            .HasDatabaseName("ix_nutrition_plan_nutritionist_patient");

        builder.HasMany(p => p.Meals)
            .WithOne()
            .HasForeignKey(m => m.NutritionPlanId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(p => p.Meals)
            .HasField("_meals")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}