using FluentValidation.TestHelper;
using NogVita.Application.Patients;
using NogVita.Domain.Users;
using NogVita.UnitTests.Fakes;

namespace NogVita.UnitTests.Application.Patients;

public class RegisterPatientRequestValidatorTests
{
    private readonly RegisterPatientRequestValidator _validator = new(TimeProvider.System);

    private static RegisterPatientRequest ValidRequest() =>
        new("João Souza", "joao@email.com", "529.982.247-25", "cafe-com-pao-de-queijo", new DateOnly(1995, 3, 10), BiologicalSex.Male, 178, Goal.MuscleGain,
            HealthDataConsent.CurrentVersion, AcceptHealthData: true, AcceptInternationalTransfer: true);

    // LGPD, arts. 11, I e 33, VIII: sem os dois aceites, na versão atual do termo, não há cadastro.
    [Fact]
    public void Should_Require_Both_Consents_In_The_Current_Version()
    {
        _validator.TestValidate(ValidRequest() with { AcceptHealthData = false }).ShouldHaveValidationErrorFor(x => x.AcceptHealthData);
        _validator.TestValidate(ValidRequest() with { AcceptInternationalTransfer = false }).ShouldHaveValidationErrorFor(x => x.AcceptInternationalTransfer);
        _validator.TestValidate(ValidRequest() with { HealthDataConsentVersion = "0" }).ShouldHaveValidationErrorFor(x => x.HealthDataConsentVersion);
        _validator.TestValidate(ValidRequest() with { HealthDataConsentVersion = null! }).ShouldHaveValidationErrorFor(x => x.HealthDataConsentVersion);
    }

    [Fact]
    public void Should_Accept_Valid_Request()
    {
        var result = _validator.TestValidate(ValidRequest());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Reject_Invalid_Cpf()
    {
        var request = ValidRequest() with { Cpf = "123.456.789-00" };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Cpf);
    }

    [Fact]
    public void Should_Not_Throw_When_Cpf_Is_Null()
    {
        var request = ValidRequest() with { Cpf = null! };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Cpf);
    }

    [Fact]
    public void Should_Reject_Weak_Password()
    {
        var request = ValidRequest() with { Password = "123456" };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Should_Reject_Future_BirthDate()
    {
        var request = ValidRequest() with { BirthDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1) };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.BirthDate);
    }

    [Theory]
    [InlineData("2008-10-11", true)]   // faz 18 anos hoje
    [InlineData("2008-10-12", false)]  // faz 18 anos amanhã
    [InlineData("2015-01-01", false)]
    public void Should_Require_Minimum_Age_Of_18(string birthDate, bool isValid)
    {
        var validator = new RegisterPatientRequestValidator(new FixedTimeProvider(new DateTimeOffset(2026, 10, 11, 12, 0, 0, TimeSpan.Zero)));
        var request = ValidRequest() with { BirthDate = DateOnly.Parse(birthDate) };

        var result = validator.TestValidate(request);

        if (isValid)
        {
            result.ShouldNotHaveValidationErrorFor(x => x.BirthDate);
        }
        else
        {
            result.ShouldHaveValidationErrorFor(x => x.BirthDate)
                .WithErrorMessage("É preciso ter pelo menos 18 anos para usar o NogVita.");
        }
    }

    [Fact]
    public void Should_Reject_Invalid_Goal()
    {
        var request = ValidRequest() with { Goal = (Goal)999 };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Goal);
    }
}
