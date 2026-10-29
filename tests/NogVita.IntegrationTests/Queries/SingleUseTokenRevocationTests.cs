using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using NogVita.Domain.Auth;
using NogVita.Domain.Common;
using NogVita.Domain.Invitations;
using NogVita.IntegrationTests.Infrastructure;

namespace NogVita.IntegrationTests.Queries;

// Fase 28: os três repositórios de tokens viraram um só (SingleUseTokenRepository<T>). A única diferença que eles
// tinham, quem revoga os tokens já expirados, virou o parâmetro revokeExpired. Estes testes prendem essa diferença
// contra o Postgres real e pelo caminho que a pessoa usa: se uma refatoração futura apagar a diferença, eles falham.
public class SingleUseTokenRevocationTests(NogVitaApiFixture fixture)
{
    private readonly TestAccounts _accounts = new(fixture);
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    private async Task<Guid> AddExpiredAsync<TToken>(Func<Guid, string, DateTime, DateTime, TToken> create, Guid userId)
        where TToken : SingleUseToken
    {
        var now = DateTime.UtcNow;
        var token = create(userId, $"hash-vencido-{Guid.NewGuid():N}", now.AddHours(-1), now.AddHours(-2));

        await using var context = fixture.CreateDbContext();
        context.Set<TToken>().Add(token);
        await context.SaveChangesAsync(_ct);
        return token.Id;
    }

    private async Task<DateTime?> RevokedAtAsync<TToken>(Guid tokenId) where TToken : SingleUseToken
    {
        await using var context = fixture.CreateDbContext();
        return (await context.Set<TToken>().SingleAsync(t => t.Id == tokenId, _ct)).RevokedAtUtc;
    }

    // A redefinição de senha deixa os expirados como estão (revokeExpired: false).
    [Fact]
    public async Task New_Password_Reset_Link_Should_Not_Touch_Expired_Tokens()
    {
        var patient = await _accounts.NewPatientAsync();
        var expiredId = await AddExpiredAsync((u, h, e, n) => new PasswordResetToken(u, h, e, n), patient.Id);

        var request = await _accounts.Anonymous().PostAsJsonAsync(
            "/api/v1/auth/password-reset/request", new { email = patient.Email }, TestAccounts.Json, _ct);

        Assert.Equal(HttpStatusCode.Accepted, request.StatusCode);
        Assert.Null(await RevokedAtAsync<PasswordResetToken>(expiredId));
    }

    // A confirmação de e-mail revoga também os expirados (revokeExpired: true).
    [Fact]
    public async Task New_Confirmation_Link_Should_Revoke_Expired_Tokens_Too()
    {
        var email = TestAccounts.UniqueEmail("sem-confirmar");
        await _accounts.Anonymous().PostAsJsonAsync("/api/v1/auth/register/patient", TestAccounts.PatientRegistration(email), TestAccounts.Json, _ct);
        Guid userId;
        await using (var context = fixture.CreateDbContext())
        {
            userId = (await context.Users.SingleAsync(u => u.Email == email, _ct)).Id;
        }
        var expiredId = await AddExpiredAsync((u, h, e, n) => new EmailConfirmationToken(u, h, e, n), userId);

        var resend = await _accounts.Anonymous().PostAsJsonAsync(
            "/api/v1/auth/email-confirmation/resend", new { email }, TestAccounts.Json, _ct);

        Assert.Equal(HttpStatusCode.Accepted, resend.StatusCode);
        Assert.NotNull(await RevokedAtAsync<EmailConfirmationToken>(expiredId));
    }

    // O convite também revoga os expirados (revokeExpired: true).
    [Fact]
    public async Task New_Invitation_Should_Revoke_Expired_Invitations_Too()
    {
        var admin = _accounts.As(await _accounts.AdminAsync());
        var preRegister = await admin.PostAsJsonAsync("/api/v1/admin/nutritionists", new
        {
            name = "Nutri do Convite Vencido",
            email = TestAccounts.UniqueEmail("convite"),
            cpf = TestAccounts.UniqueCpf(),
            crnRegion = 3,
            crnNumber = TestAccounts.UniqueCrn()
        }, TestAccounts.Json, _ct);
        var userId = (await preRegister.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>(TestAccounts.Json, _ct))
            .GetProperty("userId").GetGuid();
        var expiredId = await AddExpiredAsync((u, h, e, n) => new NutritionistInvitation(u, h, e, n), userId);

        var resend = await admin.PostAsync($"/api/v1/admin/nutritionists/{userId}/invitation", null, _ct);

        Assert.Equal(HttpStatusCode.OK, resend.StatusCode);
        Assert.NotNull(await RevokedAtAsync<NutritionistInvitation>(expiredId));
    }
}
