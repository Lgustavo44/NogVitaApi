using FluentValidation.TestHelper;
using NogVita.Application.WeightRecords;
using NogVita.Domain.WeightRecords;
using NogVita.UnitTests.Fakes;

namespace NogVita.UnitTests.Application.WeightRecords;

public class WeightRecordValidatorsTests
{
    // 14/10/2026 ao meio-dia em UTC: 9h em Brasília, então "hoje" é 14/10/2026.
    private static readonly DateOnly Today = new(2026, 10, 14);

    private readonly RecordWeightRequestValidator _recordValidator =
        new(new FixedTimeProvider(new DateTimeOffset(2026, 10, 14, 12, 0, 0, TimeSpan.Zero)));

    [Fact]
    public void Should_Accept_Weight_Without_Date()
    {
        _recordValidator.TestValidate(new RecordWeightRequest(72.4m, null, null)).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(19.99)]
    [InlineData(400.01)]
    public void Should_Refuse_Impossible_Weight(decimal weightKg)
    {
        _recordValidator.TestValidate(new RecordWeightRequest(weightKg, null, null)).ShouldHaveValidationErrorFor(r => r.WeightKg);
    }

    [Fact]
    public void Should_Accept_The_Limits()
    {
        _recordValidator.TestValidate(new RecordWeightRequest(WeightRecord.MinWeightKg, Today, null)).ShouldNotHaveAnyValidationErrors();
        _recordValidator.TestValidate(new RecordWeightRequest(WeightRecord.MaxWeightKg, Today.AddDays(-WeightRecord.MaxDaysInPast), null)).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Refuse_Future_Date_And_Too_Old_Date()
    {
        _recordValidator.TestValidate(new RecordWeightRequest(72m, Today.AddDays(1), null))
            .ShouldHaveValidationErrorFor(r => r.RecordedOn)
            .WithErrorMessage("A data precisa ser de hoje ou de até 365 dias atrás.");
        _recordValidator.TestValidate(new RecordWeightRequest(72m, Today.AddDays(-WeightRecord.MaxDaysInPast - 1), null))
            .ShouldHaveValidationErrorFor(r => r.RecordedOn);
    }

    [Fact]
    public void Should_Refuse_Long_Notes()
    {
        _recordValidator.TestValidate(new RecordWeightRequest(72m, null, new string('a', WeightRecord.NotesMaxLength + 1)))
            .ShouldHaveValidationErrorFor(r => r.Notes);
    }

    [Fact]
    public void History_Should_Refuse_End_Before_Start()
    {
        var validator = new WeightHistoryRequestValidator();

        validator.TestValidate(new WeightHistoryRequest { From = Today.AddDays(-30), To = Today }).ShouldNotHaveAnyValidationErrors();
        validator.TestValidate(new WeightHistoryRequest { From = Today, To = Today.AddDays(-1) })
            .ShouldHaveValidationErrorFor(r => r.To)
            .WithErrorMessage("A data final não pode ser anterior à inicial.");
    }
}
