using FluentValidation.TestHelper;
using NogVita.Application.Patients;

namespace NogVita.UnitTests.Application.Patients;

public class EmailConfirmationValidatorsTests
{
    [Fact]
    public void Confirmation_Should_Need_A_Token_Of_Reasonable_Size()
    {
        var validator = new ConfirmEmailRequestValidator();

        validator.TestValidate(new ConfirmEmailRequest("token")).ShouldNotHaveAnyValidationErrors();
        validator.TestValidate(new ConfirmEmailRequest("")).ShouldHaveValidationErrorFor(r => r.Token);
        validator.TestValidate(new ConfirmEmailRequest(new string('a', 201))).ShouldHaveValidationErrorFor(r => r.Token);
    }

    [Fact]
    public void Resend_Should_Need_A_Valid_Email()
    {
        var validator = new ResendEmailConfirmationRequestValidator();

        validator.TestValidate(new ResendEmailConfirmationRequest("maria@email.com")).ShouldNotHaveAnyValidationErrors();
        validator.TestValidate(new ResendEmailConfirmationRequest("")).ShouldHaveValidationErrorFor(r => r.Email);
        validator.TestValidate(new ResendEmailConfirmationRequest("sem-arroba")).ShouldHaveValidationErrorFor(r => r.Email);
    }
}
