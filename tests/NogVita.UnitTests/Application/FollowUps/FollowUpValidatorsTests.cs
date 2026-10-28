using FluentValidation.TestHelper;
using NogVita.Application.FollowUps;
using NogVita.Domain.FollowUps;

namespace NogVita.UnitTests.Application.FollowUps;

public class FollowUpValidatorsTests
{
    [Fact]
    public void Request_Should_Need_A_Nutritionist()
    {
        var validator = new RequestNutritionistRequestValidator();

        validator.TestValidate(new RequestNutritionistRequest(Guid.NewGuid(), null)).ShouldNotHaveAnyValidationErrors();
        validator.TestValidate(new RequestNutritionistRequest(Guid.Empty, null)).ShouldHaveValidationErrorFor(r => r.NutritionistId);
    }

    [Fact]
    public void Request_Message_Should_Respect_The_Limit()
    {
        var validator = new RequestNutritionistRequestValidator();
        var atLimit = new string('a', NutritionistRequest.MessageMaxLength);

        validator.TestValidate(new RequestNutritionistRequest(Guid.NewGuid(), atLimit)).ShouldNotHaveAnyValidationErrors();
        validator.TestValidate(new RequestNutritionistRequest(Guid.NewGuid(), atLimit + "a")).ShouldHaveValidationErrorFor(r => r.Message);
    }

    [Fact]
    public void Patient_Search_Should_Have_At_Most_100_Characters()
    {
        var validator = new MyPatientsListRequestValidator();

        validator.TestValidate(new MyPatientsListRequest { Search = new string('a', 100) }).ShouldNotHaveAnyValidationErrors();
        validator.TestValidate(new MyPatientsListRequest { Search = new string('a', 101) }).ShouldHaveValidationErrorFor(r => r.Search);
    }

    [Fact]
    public void Request_List_Should_Refuse_Unknown_Status()
    {
        var validator = new NutritionistRequestListRequestValidator();

        validator.TestValidate(new NutritionistRequestListRequest { Status = NutritionistRequestStatus.Pending }).ShouldNotHaveAnyValidationErrors();
        validator.TestValidate(new NutritionistRequestListRequest { Status = (NutritionistRequestStatus)99 }).ShouldHaveValidationErrorFor(r => r.Status);
    }

    // A paginação é a mesma em todas as listas (PageRequestValidator): testada aqui, por uma delas.
    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public void Pagination_Should_Refuse_Out_Of_Range_Values(int page, int pageSize)
    {
        var result = new MyPatientsListRequestValidator().TestValidate(new MyPatientsListRequest { Page = page, PageSize = pageSize });

        Assert.False(result.IsValid);
    }
}
