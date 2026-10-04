using NogVita.Domain.Common;
using NogVita.Domain.Users;

namespace NogVita.UnitTests.Domain.Users;

public class UserTests
{
    private const string ValidName = "Maria Silva";
    private const string ValidEmail = "maria@email.com";
    private const string ValidCpf = "12345678909";

    // Tipo 1: verificar um valor
    [Fact]
    public void Should_Normalize_Email_On_Creation()
    {
        var user = new User(ValidName, "  Maria@Email.COM ", ValidCpf);

        Assert.Equal("maria@email.com", user.Email);
    }

    // Tipo 2: verificar que uma regra lança exceção, com vários valores
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
        Assert.Throws<DomainException>(() => new User(ValidName, ValidEmail, cpf));
    }


    // Tipo 3: verificar uma regra que depende do estado do objeto
    [Fact]
    public void Should_Activate_User_With_Password()
    {
        var user = new User(ValidName, ValidEmail, ValidCpf);
        user.SetPasswordHash("password123");
        user.Activate();
        Assert.True(user.IsActive);
    }
    [Fact]
    public void Should_Deactivate_User()
    {
        var user = new User(ValidName, ValidEmail, ValidCpf);
        user.SetPasswordHash("password123");
        user.Activate();
        Assert.True(user.IsActive);
        user.Deactivate();
        Assert.False(user.IsActive);
    }

    [Fact]
    public void Should_Not_Activate_User_Without_Password()
    {
        var user = new User(ValidName, ValidEmail, ValidCpf);

        Assert.Throws<DomainException>(() => user.Activate());
    }

    [Fact]
    public void Should_Grant_Admin_To_Active_User()
    {
        var user = new User(ValidName, ValidEmail, ValidCpf);
        user.SetPasswordHash("hash");
        user.Activate();

        user.GrantAdmin();

        Assert.True(user.IsAdmin);
    }

    [Fact]
    public void Should_Return_Admin_Role()
    {
        var user = new User(ValidName, ValidEmail, ValidCpf);
        user.SetPasswordHash("hash");
        user.Activate();
        user.GrantAdmin();
        var roles = user.GetRoles();
        Assert.Contains(Roles.Admin, roles);
    }

    [Fact]
    public void Should_Return_Patient_Role()
    {
        var user = new User(ValidName, ValidEmail, ValidCpf);
        user.SetPasswordHash("hash");
        user.Activate();
        user.CreatePatientProfile(DateOnly.FromDateTime(DateTime.Today), BiologicalSex.Female, 170, Goal.WeightLoss);
        var roles = user.GetRoles();
        Assert.Contains(Roles.Patient, roles);
    }

    [Fact]
    public void Should_Return_Nutritionist_Role()
    {
        var user = new User(ValidName, ValidEmail, ValidCpf);
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
        var user = new User(ValidName, ValidEmail, ValidCpf);
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
        var user = new User(ValidName, ValidEmail, ValidCpf);
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
        var user = new User(ValidName, ValidEmail, ValidCpf);
        var roles = user.GetRoles();
        Assert.Empty(roles);
    }
}