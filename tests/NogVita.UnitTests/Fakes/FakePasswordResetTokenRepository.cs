using NogVita.Application.Abstractions;
using NogVita.Domain.Auth;

namespace NogVita.UnitTests.Fakes;

public sealed class FakePasswordResetTokenRepository()
    : FakeSingleUseTokenRepository<PasswordResetToken>(revokeExpired: false), IPasswordResetTokenRepository;
