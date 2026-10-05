using FluentValidation.TestHelper;
using NogVita.Application.Admin;

namespace NogVita.UnitTests.Application.Admin;

public class AdminUserListRequestValidatorTests
{
    private readonly AdminUserListRequestValidator _validator = new();

    [Fact]
    public void Should_Accept_Default_Request()
    {
        var result = _validator.TestValidate(new AdminUserListRequest());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Should_Reject_Invalid_Page(int page)
    {
        var result = _validator.TestValidate(new AdminUserListRequest { Page = page });

        result.ShouldHaveValidationErrorFor(x => x.Page);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void Should_Reject_Invalid_Page_Size(int pageSize)
    {
        var result = _validator.TestValidate(new AdminUserListRequest { PageSize = pageSize });

        result.ShouldHaveValidationErrorFor(x => x.PageSize);
    }

    [Fact]
    public void Should_Accept_Max_Page_Size()
    {
        var result = _validator.TestValidate(new AdminUserListRequest { PageSize = 100 });

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Reject_Too_Long_Search()
    {
        var result = _validator.TestValidate(new AdminUserListRequest { Search = new string('a', 101) });

        result.ShouldHaveValidationErrorFor(x => x.Search);
    }
}