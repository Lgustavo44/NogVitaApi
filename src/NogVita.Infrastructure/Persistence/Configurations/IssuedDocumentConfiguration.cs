using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NogVita.Domain.Documents;

namespace NogVita.Infrastructure.Persistence.Configurations;

public class IssuedDocumentConfiguration : IEntityTypeConfiguration<IssuedDocument>
{
    public void Configure(EntityTypeBuilder<IssuedDocument> builder)
    {
        builder.ToTable("tb_issued_document");

        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).ValueGeneratedNever();

        builder.Property(d => d.Code).HasMaxLength(IssuedDocument.CodeLength).IsRequired();
        builder.Property(d => d.Kind).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(d => d.NutritionistName).HasMaxLength(150).IsRequired();
        builder.Property(d => d.CrnNumber).HasMaxLength(20).IsRequired();
        builder.Property(d => d.Sha256).HasMaxLength(IssuedDocument.Sha256Length).IsRequired();

        builder.HasIndex(d => d.Code)
            .IsUnique()
            .HasDatabaseName("ux_issued_document_code");

        builder.HasIndex(d => new { d.Kind, d.SourceId });
    }
}
