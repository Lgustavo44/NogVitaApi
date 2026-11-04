using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NogVita.Domain.Anamneses;
using NogVita.Domain.Users;

namespace NogVita.Infrastructure.Persistence.Configurations;

public class AnamnesisConfiguration : IEntityTypeConfiguration<Anamnesis>
{
    public void Configure(EntityTypeBuilder<Anamnesis> builder)
    {
        builder.ToTable("tb_anamnesis");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();

        builder.Property(a => a.Title)
            .HasMaxLength(Questionnaire.TitleMaxLength)
            .IsRequired();

        // D-V2-31: o retrato das perguntas, sem FK para o questionário, que pode ser editado ou apagado.
        builder.Property(a => a.Questions).IsJson().IsRequired();
        builder.Property(a => a.Answers).IsJson();

        builder.Property(a => a.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(a => a.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(a => a.NutritionistId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => new { a.PatientId, a.NutritionistId })
            .HasDatabaseName("ix_anamnesis_patient_nutritionist");
    }
}
