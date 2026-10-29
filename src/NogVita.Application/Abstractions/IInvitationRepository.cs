using NogVita.Domain.Invitations;

namespace NogVita.Application.Abstractions;

public interface IInvitationRepository : ISingleUseTokenRepository<NutritionistInvitation>;
