using NogVita.Application.Abstractions;
using NogVita.Domain.Invitations;

namespace NogVita.UnitTests.Fakes;

public sealed class FakeInvitationRepository()
    : FakeSingleUseTokenRepository<NutritionistInvitation>(revokeExpired: true), IInvitationRepository
{
    // O nome que os testes já usam para a lista (e que continua igual).
    public List<NutritionistInvitation> Invitations => Tokens;
}
