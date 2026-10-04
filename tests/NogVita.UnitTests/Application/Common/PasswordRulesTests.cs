using FluentValidation;
using FluentValidation.TestHelper;
using NogVita.Application.Common.Validation;

namespace NogVita.UnitTests.Application.Common;

public class PasswordRulesTests
{
    private sealed record PasswordInput(string Password);

    private sealed class PasswordInputValidator : AbstractValidator<PasswordInput>
    {
        public PasswordInputValidator() => RuleFor(x => x.Password).StrongPassword();
    }

    private readonly PasswordInputValidator _validator = new();

    [Fact]
    public void Should_Accept_Long_Passphrase()
    {
        var result = _validator.TestValidate(new PasswordInput("cafe-com-pao-de-queijo"));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("curta")]
    [InlineData("14caracteres!!")]
    public void Should_Reject_Short_Passwords(string password)
    {
        var result = _validator.TestValidate(new PasswordInput(password));

        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Should_Reject_Too_Long_Password()
    {
        var result = _validator.TestValidate(new PasswordInput(new string('a', PasswordRules.MaxLength + 1)));

        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Should_Reject_Blocklisted_Password_Ignoring_Case()
    {
        var result = _validator.TestValidate(new PasswordInput("SenhaSenhaSenha"));

        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Should_Not_Require_Uppercase_Numbers_Or_Symbols()
    {
        var result = _validator.TestValidate(new PasswordInput("somente letras minusculas aqui"));

        result.ShouldNotHaveAnyValidationErrors();
    }
}