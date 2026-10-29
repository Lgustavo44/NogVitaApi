using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NogVita.Infrastructure.Persistence.Seed;

namespace NogVita.Infrastructure.Persistence.Configurations;

public class SeedRunConfiguration : IEntityTypeConfiguration<SeedRun>
{
    public void Configure(EntityTypeBuilder<SeedRun> builder)
    {
        builder.ToTable("tb_seed_run");

        builder.HasKey(s => s.Name);
        builder.Property(s => s.Name).HasMaxLength(60);

        // SHA-256 em hexadecimal: 64 caracteres.
        builder.Property(s => s.ContentHash).HasMaxLength(64).IsRequired();
        builder.Property(s => s.CompletedAtUtc).IsRequired();
    }
}
