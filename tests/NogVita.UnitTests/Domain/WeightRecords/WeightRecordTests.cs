using NogVita.Domain.Common;
using NogVita.Domain.WeightRecords;

namespace NogVita.UnitTests.Domain.WeightRecords;

public class WeightRecordTests
{
    private static readonly Guid PatientId = Guid.NewGuid();
    private static readonly Guid NutritionistId = Guid.NewGuid();
    private static readonly DateOnly Today = new(2026, 10, 17);

    [Fact]
    public void Patient_Should_Record_Own_Weight()
    {
        var record = new WeightRecord(PatientId, PatientId, Today, 78.456m, "  Em jejum.  ", Today);

        Assert.Equal(78.46m, record.WeightKg);
        Assert.Equal("Em jejum.", record.Notes);
        Assert.True(record.WasRecordedByPatient);
    }

    [Fact]
    public void Nutritionist_Record_Should_Not_Be_Marked_As_Patient()
    {
        var record = new WeightRecord(PatientId, NutritionistId, Today, 80m, null, Today);

        Assert.False(record.WasRecordedByPatient);
        Assert.True(record.WasRecordedBy(NutritionistId));
    }

    [Fact]
    public void Should_Accept_Past_Date_Within_One_Year()
    {
        var record = new WeightRecord(PatientId, PatientId, Today.AddDays(-WeightRecord.MaxDaysInPast), 80m, null, Today);

        Assert.Equal(Today.AddDays(-365), record.RecordedOn);
    }

    [Fact]
    public void Should_Reject_Future_Date()
    {
        Assert.Throws<DomainException>(() => new WeightRecord(PatientId, PatientId, Today.AddDays(1), 80m, null, Today));
    }

    [Fact]
    public void Should_Reject_Date_Older_Than_One_Year()
    {
        Assert.Throws<DomainException>(() =>
            new WeightRecord(PatientId, PatientId, Today.AddDays(-WeightRecord.MaxDaysInPast - 1), 80m, null, Today));
    }

    [Theory]
    [InlineData(19.99)]
    [InlineData(400.01)]
    [InlineData(800)]
    public void Should_Reject_Impossible_Weight(double weight)
    {
        Assert.Throws<DomainException>(() => new WeightRecord(PatientId, PatientId, Today, (decimal)weight, null, Today));
    }

    [Fact]
    public void Author_Should_Update_Own_Record()
    {
        var record = new WeightRecord(PatientId, PatientId, Today, 80m, null, Today);

        record.Update(PatientId, 79.5m, "Depois do treino.");

        Assert.Equal(79.5m, record.WeightKg);
        Assert.Equal("Depois do treino.", record.Notes);
    }

    [Fact]
    public void Should_Not_Update_Record_Of_Someone_Else()
    {
        var record = new WeightRecord(PatientId, NutritionistId, Today, 80m, null, Today);

        Assert.Throws<DomainException>(() => record.Update(PatientId, 79m, null));
        Assert.Equal(80m, record.WeightKg);
    }

    [Fact]
    public void Rejected_Update_Should_Not_Change_Anything()
    {
        var record = new WeightRecord(PatientId, PatientId, Today, 80m, "Antes", Today);
        var tooLongNotes = new string('a', WeightRecord.NotesMaxLength + 1);

        Assert.Throws<DomainException>(() => record.Update(PatientId, 75m, tooLongNotes));

        Assert.Equal(80m, record.WeightKg);
        Assert.Equal("Antes", record.Notes);
    }
}
