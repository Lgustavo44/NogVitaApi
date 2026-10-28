using FluentValidation.TestHelper;
using NogVita.Application.Foods;
using NogVita.Domain.Foods;

namespace NogVita.UnitTests.Application.Foods;

public class FoodSearchRequestValidatorTests
{
    private readonly FoodSearchRequestValidator _validator = new();

    [Fact]
    public void Should_Accept_Two_Letters()
    {
        _validator.TestValidate(new FoodSearchRequest { Search = "ov", Source = FoodSource.Taco }).ShouldNotHaveAnyValidationErrors();
    }

    // Os espaços não contam: " a " é uma letra só.
    [Theory]
    [InlineData("")]
    [InlineData("a")]
    [InlineData(" a ")]
    [InlineData("   ")]
    public void Should_Need_At_Least_Two_Characters(string search)
    {
        _validator.TestValidate(new FoodSearchRequest { Search = search })
            .ShouldHaveValidationErrorFor(r => r.Search)
            .WithErrorMessage("Digite pelo menos 2 caracteres para buscar.");
    }

    [Fact]
    public void Should_Refuse_Long_Search_And_Unknown_Source()
    {
        _validator.TestValidate(new FoodSearchRequest { Search = new string('a', 101) }).ShouldHaveValidationErrorFor(r => r.Search);
        _validator.TestValidate(new FoodSearchRequest { Search = "arroz", Source = (FoodSource)99 }).ShouldHaveValidationErrorFor(r => r.Source);
    }
}
