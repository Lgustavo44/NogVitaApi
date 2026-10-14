using FluentValidation.TestHelper;
using NogVita.Application.Patients;
using NogVita.Domain.Users;
using NogVita.UnitTests.Fakes;

namespace NogVita.UnitTests.Application.Patients;

public class UpdatePatientProfileRequestValidatorTests
{
    private readonly UpdatePatientProfileRequestValidator _validator =
        new(new FixedTimeProvider(new DateTimeOffset(2026, 10, 11, 12, 0, 0, TimeSpan.Zero)));

    private static UpdatePatientProfileRequest ValidRequest() =>
        new(new DateOnly(1995, 3, 10), BiologicalSex.Female, 165, Goal.Maintenance);

    [Fact]
    public void Should_Accept_Valid_Request()
    {
        var result = _validator.TestValidate(ValidRequest());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Reject_Future_BirthDate()
    {
        var result = _validator.TestValidate(ValidRequest() with { BirthDate = new DateOnly(2026, 10, 12) });

        result.ShouldHaveValidationErrorFor(x => x.BirthDate)
            .WithErrorMessage("A data de nascimento não pode estar no futuro.");
    }

    [Fact]
    public void Should_Reject_BirthDate_Under_18()
    {
        var result = _validator.TestValidate(ValidRequest() with { BirthDate = new DateOnly(2008, 10, 12) });

        result.ShouldHaveValidationErrorFor(x => x.BirthDate)
            .WithErrorMessage("É preciso ter pelo menos 18 anos para usar o NogVita.");
    }

    [Fact]
    public void Should_Reject_Invalid_Height()
    {
        var result = _validator.TestValidate(ValidRequest() with { HeightInCm = 0 });

        result.ShouldHaveValidationErrorFor(x => x.HeightInCm);
    }

    [Fact]
    public void Should_Reject_Invalid_Enums()
    {
        var result = _validator.TestValidate(ValidRequest() with { BiologicalSex = (BiologicalSex)999, Goal = (Goal)999 });

        result.ShouldHaveValidationErrorFor(x => x.BiologicalSex);
        result.ShouldHaveValidationErrorFor(x => x.Goal);
    }

    [Fact]
    public void Should_Accept_Night_Shift_Routine()
    {
        var result = _validator.TestValidate(ValidRequest() with { WakeUpTime = new TimeOnly(14, 0), BedTime = new TimeOnly(6, 0) });

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Reject_Same_Wake_Up_And_Bed_Time()
    {
        var result = _validator.TestValidate(ValidRequest() with { WakeUpTime = new TimeOnly(7, 0), BedTime = new TimeOnly(7, 0) });

        result.ShouldHaveValidationErrorFor(x => x.BedTime)
            .WithErrorMessage("A hora de acordar e a de dormir não podem ser iguais.");
    }
}
