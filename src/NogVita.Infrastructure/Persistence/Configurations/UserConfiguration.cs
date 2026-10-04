using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NogVita.Domain.Users;

namespace NogVita.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("tb_user");

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).ValueGeneratedNever();

        builder.Property(u => u.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(u => u.Email)
            .HasMaxLength(254)
            .IsRequired();

        builder.Property(u => u.Cpf)
            .HasConversion(cpf => cpf.Value, value => Cpf.Create(value))
            .HasMaxLength(11)
            .IsRequired();

        builder.Property(u => u.PasswordHash)
            .IsRequired(false);

        builder.HasIndex(u => u.Email)
            .IsUnique();
        builder.HasIndex(u => u.Cpf)
            .IsUnique();    

        builder.HasOne(u => u.PatientProfile)
            .WithOne()
            .HasForeignKey<PatientProfile>(p => p.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(u => u.NutritionistProfile)
            .WithOne()
            .HasForeignKey<NutritionistProfile>(p => p.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}