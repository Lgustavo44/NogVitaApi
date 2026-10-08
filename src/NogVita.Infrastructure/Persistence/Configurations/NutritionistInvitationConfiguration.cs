using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NogVita.Domain.Auth;
using NogVita.Domain.Invitations;
using NogVita.Domain.Users;

namespace NogVita.Infrastructure.Persistence.Configurations;

public class NutritionistInvitationConfiguration : IEntityTypeConfiguration<NutritionistInvitation>
{
    public void Configure(EntityTypeBuilder<NutritionistInvitation> builder)
    {
        // TODO: siga o RefreshTokenConfiguration, trocando:
        //   - a tabela para "tb_nutritionist_invitation"
        //   - ExpiresAtUtc obrigatório; UsedAtUtc e RevokedAtUtc opcionais
        //   - índice ÚNICO no TokenHash, índice comum no UserId
        //   - a FK para o User com Restrict

        builder.ToTable("tb_nutritionist_invitation");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();
        builder.Property(t => t.TokenHash)
            .HasMaxLength(64)
            .IsRequired();
        builder.Property(t => t.ExpiresAtUtc)
            .IsRequired();
        builder.Property(t => t.RevokedAtUtc)
            .IsRequired(false);
        builder.Property(t => t.UsedAtUtc)
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