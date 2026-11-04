using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NogVita.Domain.Anamneses;
using NogVita.Domain.Users;

namespace NogVita.Infrastructure.Persistence.Configurations;

public class QuestionnaireConfiguration : IEntityTypeConfiguration<Questionnaire>
{
    public void Configure(EntityTypeBuilder<Questionnaire> builder)
    {
        builder.ToTable("tb_questionnaire");

        builder.HasKey(q => q.Id);
        builder.Property(q => q.Id).ValueGeneratedNever();

        builder.Property(q => q.Title)
            .HasMaxLength(Questionnaire.TitleMaxLength)
            .IsRequired();

        builder.Property(q => q.Questions).IsJson().IsRequired();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(q => q.NutritionistId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(q => q.NutritionistId)
            .HasDatabaseName("ix_questionnaire_nutritionist");
    }
}
