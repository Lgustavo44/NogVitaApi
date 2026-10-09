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
        SingleUseTokenMapping.Configure(builder, "tb_nutritionist_invitation");
    }
}