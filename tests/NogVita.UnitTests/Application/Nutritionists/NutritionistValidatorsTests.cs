using FluentValidation.TestHelper;
using NogVita.Application.Nutritionists;
using NogVita.Domain.Users;

namespace NogVita.UnitTests.Application.Nutritionists;

public class NutritionistValidatorsTests
{
    [Fact]
    public void Invitation_Should_Need_A_Token()
    {
        var validator = new AcceptInvitationRequestValidator();

        validator.TestValidate(new AcceptInvitationRequest("token", null)).ShouldNotHaveAnyValidationErrors();
        validator.TestValidate(new AcceptInvitationRequest("", null)).ShouldHaveValidationErrorFor(r => r.Token);
        validator.TestValidate(new AcceptInvitationRequest(new string('a', 129), null)).ShouldHaveValidationErrorFor(r => r.Token);
    }

    // A senha é opcional (quem já é paciente não manda), mas, quando vem, segue a política.
    [Fact]
    public void Invitation_Password_Should_Follow_The_Policy_Only_When_Sent()
    {
        var validator = new AcceptInvitationRequestValidator();

        validator.TestValidate(new AcceptInvitationRequest("token", "uma senha bem longa aqui")).ShouldNotHaveAnyValidationErrors();
        validator.TestValidate(new AcceptInvitationRequest("token", "curta")).ShouldHaveValidationErrorFor(r => r.Password);
    }

    [Fact]
    public void Bio_Should_Respect_The_Limit()
    {
        var validator = new UpdateMyNutritionistProfileRequestValidator();
        var atLimit = new string('a', NutritionistProfile.BioMaxLength);

        validator.TestValidate(new UpdateMyNutritionistProfileRequest(null)).ShouldNotHaveAnyValidationErrors();
        validator.TestValidate(new UpdateMyNutritionistProfileRequest(atLimit)).ShouldNotHaveAnyValidationErrors();
        validator.TestValidate(new UpdateMyNutritionistProfileRequest(atLimit + "a")).ShouldHaveValidationErrorFor(r => r.Bio);
    }

    [Fact]
    public void Nutritionist_List_Should_Refuse_Invalid_Filters()
    {
        var validator = new NutritionistListRequestValidator();

        validator.TestValidate(new NutritionistListRequest { Search = "ana", CrnRegion = 3 }).ShouldNotHaveAnyValidationErrors();
        validator.TestValidate(new NutritionistListRequest { CrnRegion = 0 }).ShouldHaveValidationErrorFor(r => r.CrnRegion);
        validator.TestValidate(new NutritionistListRequest { Search = new string('a', 101) }).ShouldHaveValidationErrorFor(r => r.Search);
    }
}
