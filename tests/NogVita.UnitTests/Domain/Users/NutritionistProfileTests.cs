using NogVita.Domain.Common;
using NogVita.Domain.Users;

namespace NogVita.UnitTests.Domain.Users;

public class NutritionistProfileTests
{
    private const int ValidCrnRegion = 10;
    private const string ValidCrnNumber = "12345";

    private static User CreateUser() => new("Maria Silva", "maria@email.com", TestData.ValidCpf);

    [Fact]
    public void Should_Create_Nutritionist_Profile_Inactive()
    {
        var user = CreateUser();

        var profile = user.CreateNutritionistProfile(ValidCrnRegion, ValidCrnNumber);

        Assert.NotNull(user.NutritionistProfile);
        Assert.False(profile.IsActive);
    }

    [Fact]
    public void Should_Not_Create_Second_Nutritionist_Profile()
    {
        var user = CreateUser();
        user.CreateNutritionistProfile(ValidCrnRegion, ValidCrnNumber);

        Assert.Throws<DomainException>(() => user.CreateNutritionistProfile(ValidCrnRegion, "99999"));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(11)]
    public void Should_Create_Nutritionist_With_Boundary_Crn_Region(int crnRegion)
    {
        var user = CreateUser();

        var profile = user.CreateNutritionistProfile(crnRegion, ValidCrnNumber);

        Assert.Equal(crnRegion, profile.CrnRegion);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(12)]
    public void Should_Not_Create_Nutritionist_With_Invalid_Crn_Region(int crnRegion)
    {
        var user = CreateUser();

        Assert.Throws<DomainException>(() => user.CreateNutritionistProfile(crnRegion, ValidCrnNumber));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Should_Not_Create_Nutritionist_Without_Crn_Number(string crnNumber)
    {
        var user = CreateUser();

        Assert.Throws<DomainException>(() => user.CreateNutritionistProfile(ValidCrnRegion, crnNumber));
    }

    [Fact]
    public void Should_Activate_Nutritionist_Profile()
    {
        var user = CreateUser();
        var profile = user.CreateNutritionistProfile(ValidCrnRegion, ValidCrnNumber);

        profile.Activate();

        Assert.True(profile.IsActive);
    }

    [Fact]
    public void Should_Allow_User_To_Have_Both_Profiles()
    {
        var user = CreateUser();

        user.CreatePatientProfile(new DateOnly(1990, 5, 20), BiologicalSex.Female, 165, Goal.Maintenance);
        user.CreateNutritionistProfile(ValidCrnRegion, ValidCrnNumber);

        Assert.NotNull(user.PatientProfile);
        Assert.NotNull(user.NutritionistProfile);
    }
}