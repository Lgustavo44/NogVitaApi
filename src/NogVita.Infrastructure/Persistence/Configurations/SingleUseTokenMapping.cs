using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NogVita.Domain.Common;
using NogVita.Domain.Users;

namespace NogVita.Infrastructure.Persistence.Configurations;

internal static class SingleUseTokenMapping
{
    public static void Configure<T>(EntityTypeBuilder<T> builder, string tableName) where T : SingleUseToken
    {
        builder.ToTable(tableName);

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();

        builder.Property(t => t.TokenHash)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(t => t.ExpiresAtUtc)
            .IsRequired();

        builder.Property(t => t.UsedAtUtc)
            .IsRequired(false);

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