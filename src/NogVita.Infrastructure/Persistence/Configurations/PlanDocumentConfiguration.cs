using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NogVita.Domain.NutritionPlans;

namespace NogVita.Infrastructure.Persistence.Configurations;

public class PlanDocumentConfiguration : IEntityTypeConfiguration<PlanDocument>
{
    public void Configure(EntityTypeBuilder<PlanDocument> builder)
    {
        builder.ToTable("tb_plan_document");

        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).ValueGeneratedNever();

        builder.Property(d => d.Code).HasMaxLength(PlanDocument.CodeLength).IsRequired();
        builder.Property(d => d.NutritionistName).HasMaxLength(150).IsRequired();
        builder.Property(d => d.CrnNumber).HasMaxLength(20).IsRequired();
        builder.Property(d => d.Sha256).HasMaxLength(PlanDocument.Sha256Length).IsRequired();

        builder.HasOne<NutritionPlan>()
            .WithMany()
            .HasForeignKey(d => d.PlanId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(d => d.Code)
            .IsUnique()
            .HasDatabaseName("ux_plan_document_code");
    }
}
