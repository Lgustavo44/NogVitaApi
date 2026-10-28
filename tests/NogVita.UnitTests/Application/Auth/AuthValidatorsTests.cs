using FluentValidation.TestHelper;
using NogVita.Application.Auth;

namespace NogVita.UnitTests.Application.Auth;

public class AuthValidatorsTests
{
    private const string StrongPassword = "uma-senha-nova-bem-longa";

    [Fact]
    public void Password_Reset_Request_Should_Need_A_Valid_Email()
    {
        var validator = new RequestPasswordResetRequestValidator();

        validator.TestValidate(new RequestPasswordResetRequest("maria@email.com")).ShouldNotHaveAnyValidationErrors();
        validator.TestValidate(new RequestPasswordResetRequest("")).ShouldHaveValidationErrorFor(r => r.Email);
        validator.TestValidate(new RequestPasswordResetRequest("sem-arroba")).ShouldHaveValidationErrorFor(r => r.Email);
    }

    [Fact]
    public void Password_Reset_Confirmation_Should_Apply_The_Password_Policy()
    {
        var validator = new ConfirmPasswordResetRequestValidator();

        validator.TestValidate(new ConfirmPasswordResetRequest("token", StrongPassword)).ShouldNotHaveAnyValidationErrors();
        validator.TestValidate(new ConfirmPasswordResetRequest("token", "curta")).ShouldHaveValidationErrorFor(r => r.NewPassword);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Password_Reset_Confirmation_Should_Need_A_Token(string? token)
    {
        new ConfirmPasswordResetRequestValidator()
            .TestValidate(new ConfirmPasswordResetRequest(token!, StrongPassword))
            .ShouldHaveValidationErrorFor(r => r.Token);
    }

    [Fact]
    public void Password_Reset_Confirmation_Should_Refuse_A_Giant_Token()
    {
        new ConfirmPasswordResetRequestValidator()
            .TestValidate(new ConfirmPasswordResetRequest(new string('a', 201), StrongPassword))
            .ShouldHaveValidationErrorFor(r => r.Token);
    }

    [Fact]
    public void Refresh_And_Logout_Should_Need_A_Refresh_Token_Of_Reasonable_Size()
    {
        var giant = new string('a', RefreshTokenRules.MaxLength + 1);

        new RefreshRequestValidator().TestValidate(new RefreshRequest("token-valido")).ShouldNotHaveAnyValidationErrors();
        new RefreshRequestValidator().TestValidate(new RefreshRequest("")).ShouldHaveValidationErrorFor(r => r.RefreshToken);
        new RefreshRequestValidator().TestValidate(new RefreshRequest(giant)).ShouldHaveValidationErrorFor(r => r.RefreshToken);

        new LogoutRequestValidator().TestValidate(new LogoutRequest("token-valido")).ShouldNotHaveAnyValidationErrors();
        new LogoutRequestValidator().TestValidate(new LogoutRequest("")).ShouldHaveValidationErrorFor(r => r.RefreshToken);
        new LogoutRequestValidator().TestValidate(new LogoutRequest(giant)).ShouldHaveValidationErrorFor(r => r.RefreshToken);
    }
}
