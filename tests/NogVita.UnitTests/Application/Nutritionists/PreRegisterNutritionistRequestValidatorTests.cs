using FluentValidation.TestHelper;
using NogVita.Application.Nutritionists;

namespace NogVita.UnitTests.Application.Nutritionists;

public class PreRegisterNutritionistRequestValidatorTests
{
    private readonly PreRegisterNutritionistRequestValidator _validator = new();

    private static PreRegisterNutritionistRequest ValidRequest() =>
        new("Ana Lima", "ana@nogvita.local", "123.456.789-09", 3, "12345");

    [Fact]
    public void Should_Accept_Valid_Request()
    {
        var result = _validator.TestValidate(ValidRequest());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(11)]
    public void Should_Accept_Boundary_Crn_Regions(int crnRegion)
    {
        var result = _validator.TestValidate(ValidRequest() with { CrnRegion = crnRegion });

        result.ShouldNotHaveValidationErrorFor(x => x.CrnRegion);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(12)]
    public void Should_Reject_Invalid_Crn_Region(int crnRegion)
    {
        var result = _validator.TestValidate(ValidRequest() with { CrnRegion = crnRegion });

        result.ShouldHaveValidationErrorFor(x => x.CrnRegion);
    }

    [Fact]
    public void Should_Reject_Invalid_Cpf()
    {
        var result = _validator.TestValidate(ValidRequest() with { Cpf = "111.111.111-11" });

        result.ShouldHaveValidationErrorFor(x => x.Cpf);
    }

    [Fact]
    public void Should_Reject_Empty_Crn_Number()
    {
        var result = _validator.TestValidate(ValidRequest() with { CrnNumber = "" });

        result.ShouldHaveValidationErrorFor(x => x.CrnNumber);
    }
}