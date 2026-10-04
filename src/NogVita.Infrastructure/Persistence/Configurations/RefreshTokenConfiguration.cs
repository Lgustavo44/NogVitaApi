using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NogVita.Domain.Auth;
using NogVita.Domain.Users;

namespace NogVita.Infrastructure.Persistence.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("tb_refresh_token");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();

        builder.Property(t => t.TokenHash)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(t => t.ExpiresAtUtc)
            .IsRequired();
        builder.Property(t => t.RevokedAtUtc)
            .IsRequired(false);
        builder.HasIndex(t => t.TokenHash)
            .IsUnique();
        builder.HasIndex(t => t.UserId)
            .IsUnique(false);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}