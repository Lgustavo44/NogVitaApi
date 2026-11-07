using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NogVita.Domain.FoodDiary;
using NogVita.Domain.Users;

namespace NogVita.Infrastructure.Persistence.Configurations;

public class DiaryDayConfiguration : IEntityTypeConfiguration<DiaryDay>
{
    public void Configure(EntityTypeBuilder<DiaryDay> builder)
    {
        builder.ToTable("tb_diary_day");

        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).ValueGeneratedNever();

        // D-V2-37: os check-ins e os extras são retratos que ninguém filtra por dentro, como as respostas da anamnese.
        builder.Property(d => d.MealCheckIns).IsJson().IsRequired();
        builder.Property(d => d.ExtraFoods).IsJson().IsRequired();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(d => d.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(d => new { d.PatientId, d.Date })
            .IsUnique()
            .HasDatabaseName("ux_diary_day_patient_date");
    }
}

public class WeeklyCheckInConfiguration : IEntityTypeConfiguration<WeeklyCheckIn>
{
    public void Configure(EntityTypeBuilder<WeeklyCheckIn> builder)
    {
        builder.ToTable("tb_weekly_check_in");

        builder.HasKey(w => w.Id);
        builder.Property(w => w.Id).ValueGeneratedNever();

        builder.Property(w => w.Difficulties).HasMaxLength(WeeklyCheckIn.TextMaxLength);
        builder.Property(w => w.Comment).HasMaxLength(WeeklyCheckIn.TextMaxLength);
        builder.Property(w => w.Feedback).HasMaxLength(WeeklyCheckIn.TextMaxLength);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(w => w.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(w => w.NutritionistId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(w => new { w.PatientId, w.WeekStart })
            .IsUnique()
            .HasDatabaseName("ux_weekly_check_in_patient_week");

        builder.HasIndex(w => new { w.NutritionistId, w.Feedback })
            .HasDatabaseName("ix_weekly_check_in_awaiting");
    }
}
