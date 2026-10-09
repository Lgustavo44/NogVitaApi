using NogVita.Domain.Common;
using NogVita.Domain.Users;

namespace NogVita.UnitTests.Domain.Users;

public class UserTests
{
    private const string ValidName = "Maria Silva";
    private const string ValidEmail = "maria@email.com";
    private static readonly Cpf ValidCpf = TestData.ValidCpf;

    [Fact]
    public void Should_Normalize_Email_On_Creation()
    {
        var user = TestData.CreateUser();

        Assert.Equal("maria@email.com", user.Email);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Should_Not_Create_User_Without_Email(string email)
    {
        Assert.Throws<DomainException>(() => new User(ValidName, email, ValidCpf));
    }
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Should_Not_Create_User_Without_Name(string name)
    {
        Assert.Throws<DomainException>(() => new User(name, ValidEmail, ValidCpf));
    }
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Should_Not_Create_User_Without_CPF(string cpf)
    {
        Assert.Throws<DomainException>(() => new User(ValidName, ValidEmail, Cpf.Create(cpf)));
    }


    // Tipo 3: verificar uma regra que depende do estado do objeto
    [Fact]
    public void Should_Activate_User_With_Password()
    {
        var user = TestData.CreateUser();
        user.SetPasswordHash("password123");
        user.Activate();
        Assert.True(user.IsActive);
    }
    [Fact]
    public void Should_Deactivate_User()
    {
        var user = TestData.CreateUser();
        user.SetPasswordHash("password123");
        user.Activate();
        Assert.True(user.IsActive);
        user.Deactivate();
        Assert.False(user.IsActive);
    }

    [Fact]
    public void Should_Not_Activate_User_Without_Password()
    {
        var user = TestData.CreateUser();

        Assert.Throws<DomainException>(() => user.Activate());
    }

    [Fact]
    public void Should_Grant_Admin_To_Active_User()
    {
        var user = TestData.CreateUser();
        user.SetPasswordHash("hash");
        user.Activate();

        user.GrantAdmin();

        Assert.True(user.IsAdmin);
    }

    [Fact]
    public void Should_Return_Admin_Role()
    {
        var user = TestData.CreateUser();
        user.SetPasswordHash("hash");
        user.Activate();
        user.GrantAdmin();
        var roles = user.GetRoles();
        Assert.Contains(Roles.Admin, roles);
    }

    [Fact]
    public void Should_Return_Patient_Role()
    {
        var user = TestData.CreateUser();
        user.SetPasswordHash("hash");
        user.Activate();
        user.CreatePatientProfile(DateOnly.FromDateTime(DateTime.Today), BiologicalSex.Female, 170, Goal.WeightLoss);
        var roles = user.GetRoles();
        Assert.Contains(Roles.Patient, roles);
    }

    [Fact]
    public void Should_Return_Nutritionist_Role()
    {
        var user = TestData.CreateUser();
        user.SetPasswordHash("hash");
        user.Activate();
        var profile = user.CreateNutritionistProfile(1, "123456");
        profile.Activate();

        var roles = user.GetRoles();

        Assert.Contains(Roles.Nutritionist, roles);
    }



    [Fact]
    public void Should_Not_Return_Nutritionist_Role_When_Profile_Inactive()
    {
        var user = TestData.CreateUser();
        user.SetPasswordHash("hash");
        user.Activate();
        var nutritionistProfile = user.CreateNutritionistProfile(1, "123456");
        nutritionistProfile.Deactivate();
        var roles = user.GetRoles();
        Assert.DoesNotContain(Roles.Nutritionist, roles);
    }

    [Fact]
    public void Should_Return_Nutritionist_Role_When_Profile_Active()
    {
        var user = TestData.CreateUser();
        user.SetPasswordHash("hash");
        user.Activate();
        var nutritionistProfile = user.CreateNutritionistProfile(1, "123456");
        nutritionistProfile.Activate();
        var roles = user.GetRoles();
        Assert.Contains(Roles.Nutritionist, roles);
    }

    [Fact]
    public void Should_Return_No_Roles_For_New_User()
    {
        var user = TestData.CreateUser();
        var roles = user.GetRoles();
        Assert.Empty(roles);
    }

    [Fact]
    public void Should_Update_Nutritionist_Bio()
    {
        var user = TestData.CreateUser();
        user.CreateNutritionistProfile(1, "123456");

        user.UpdateNutritionistProfile("  Nutrição esportiva e emagrecimento.  ");

        Assert.Equal("Nutrição esportiva e emagrecimento.", user.NutritionistProfile!.Bio);
    }

    [Fact]
    public void Should_Store_Null_When_Bio_Is_Blank()
    {
        var user = TestData.CreateUser();
        user.CreateNutritionistProfile(1, "123456");
        user.UpdateNutritionistProfile("Texto antigo");

        user.UpdateNutritionistProfile("   ");

        Assert.Null(user.NutritionistProfile!.Bio);
    }

    [Fact]
    public void Should_Not_Accept_Bio_Longer_Than_Limit()
    {
        var user = TestData.CreateUser();
        user.CreateNutritionistProfile(1, "123456");

        var bio = new string('a', NutritionistProfile.BioMaxLength + 1);

        Assert.Throws<DomainException>(() => user.UpdateNutritionistProfile(bio));
    }

    [Fact]
    public void Should_Not_Update_Bio_Without_Nutritionist_Profile()
    {
        var user = TestData.CreateUser();

        Assert.Throws<DomainException>(() => user.UpdateNutritionistProfile("Bio"));
    }
}