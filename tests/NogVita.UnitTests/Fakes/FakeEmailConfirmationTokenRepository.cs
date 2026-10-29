using NogVita.Application.Abstractions;
using NogVita.Domain.Auth;

namespace NogVita.UnitTests.Fakes;

public sealed class FakeEmailConfirmationTokenRepository()
    : FakeSingleUseTokenRepository<EmailConfirmationToken>(revokeExpired: true), IEmailConfirmationTokenRepository;
