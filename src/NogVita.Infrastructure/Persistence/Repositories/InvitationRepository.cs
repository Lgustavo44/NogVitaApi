using NogVita.Application.Abstractions;
using NogVita.Domain.Invitations;

namespace NogVita.Infrastructure.Persistence.Repositories;

public sealed class InvitationRepository(NogVitaDbContext context)
    : SingleUseTokenRepository<NutritionistInvitation>(context, revokeExpired: true), IInvitationRepository;
