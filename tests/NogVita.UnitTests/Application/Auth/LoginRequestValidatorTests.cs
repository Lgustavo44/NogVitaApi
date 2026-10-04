using FluentValidation.TestHelper;
using NogVita.Application.Auth;

namespace NogVita.UnitTests.Application.Auth;

public class LoginRequestValidatorTests
{
    private readonly LoginRequestValidator _validator = new();

    [Fact]
    public void Should_Accept_Valid_Request()
    {
        var result = _validator.TestValidate(new LoginRequest("maria@email.com", "qualquer-senha"));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Reject_Invalid_Email()
    {
        var result = _validator.TestValidate(new LoginRequest("email-invalido", "qualquer-senha"));
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Should_Not_Apply_Password_Policy_On_Login()
    {
        var result = _validator.TestValidate(new LoginRequest("maria@email.com", "senha-curta"));

        result.ShouldNotHaveAnyValidationErrors();
    }
    [Fact]
    public void Should_Reject_Empty_Password()
    {
        var result = _validator.TestValidate(new LoginRequest("maria@email.com", ""));
        result.ShouldHaveValidationErrorFor(x => x.Password);
    } 
    

}