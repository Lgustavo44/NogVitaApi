using NogVita.Domain.Common;
using NogVita.Domain.Users;

namespace NogVita.UnitTests.Domain.Users;

public class PatientProfileTests
{
    [Fact]
    public void Should_Not_Update_When_User_Has_No_Patient_Profile()
    {
        var user = new User("Maria Silva", "maria@email.com", TestData.ValidCpf);

        Assert.Throws<DomainException>(() => user.UpdatePatientProfile(DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-31), BiologicalSex.Female, 165, Goal.WeightLoss));
    }

    [Theory]
    [InlineData("1995-03-10", "2026-03-10", 31)]
    [InlineData("1995-03-10", "2026-03-09", 30)]
    [InlineData("1995-03-10", "2026-12-31", 31)]
    [InlineData("2000-02-29", "2026-02-28", 25)]
    public void Should_Calculate_Age_At_Date(string birthDate, string date, int expectedAge)
    {
        var user = new User("Maria Silva", "maria@email.com", Cpf.Create("12345678909"));
        var profile = user.CreatePatientProfile(DateOnly.Parse(birthDate), BiologicalSex.Female, 165, Goal.Maintenance);

        var age = profile.GetAgeAt(DateOnly.Parse(date));

        Assert.Equal(expectedAge, age);
    }

    [Fact]
    public void Should_Not_Update_With_Future_BirthDate()
    {
        var user = new User("Maria Silva", "maria@email.com", TestData.ValidCpf);
        var tomorrow = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);

        Assert.Throws<DomainException>(() => user.UpdatePatientProfile(tomorrow, BiologicalSex.Female, 165, Goal.WeightLoss));
    }
    [Fact]
    public void Should_Not_Update_With_Invalid_Height()
    {
        var user = new User("Maria Silva", "maria@email.com", TestData.ValidCpf);
        var patientProfile = user.CreatePatientProfile(DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-30), BiologicalSex.Female, 165, Goal.WeightLoss);

        Assert.Throws<DomainException>(() => user.UpdatePatientProfile(DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-31), BiologicalSex.Female, 0, Goal.WeightLoss));
    }
    [Fact]
    public void Should_Keep_Profile_Id_On_Update()
    {
        var user = new User("Maria Silva", "maria@email.com", TestData.ValidCpf);
        var patientProfile = user.CreatePatientProfile(DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-30), BiologicalSex.Female, 165, Goal.WeightLoss);

        var originalId = patientProfile.Id;

        user.UpdatePatientProfile(DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-31), BiologicalSex.Female, 165, Goal.WeightLoss);

        Assert.Equal(originalId, patientProfile.Id);
    }
    [Fact]
    public void Should_Update_Patient_Profile()
    {
        var user = new User("Maria Silva", "maria@email.com", TestData.ValidCpf);
        var patientProfile = user.CreatePatientProfile(DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-30), BiologicalSex.Female, 165, Goal.WeightLoss);

        user.UpdatePatientProfile(DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-31), BiologicalSex.Female, 165, Goal.WeightLoss);
    }

    [Fact]
    public void Should_Not_Create_Patient_With_Future_BirthDate()
    {
        var user = new User("Maria Silva", "maria@email.com", TestData.ValidCpf);
        var tomorrow = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);

        Assert.Throws<DomainException>(() =>
            user.CreatePatientProfile(tomorrow, BiologicalSex.Female, 165, Goal.WeightLoss));
    }

    [Fact]
    public void Should_Create_Patient_On_18th_Birthday()
    {
        var user = new User("Maria Silva", "maria@email.com", TestData.ValidCpf);
        var eighteenToday = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-PatientProfile.MinimumAge);

        var profile = user.CreatePatientProfile(eighteenToday, BiologicalSex.Female, 165, Goal.WeightLoss);

        Assert.Equal(eighteenToday, profile.BirthDate);
    }

    [Fact]
    public void Should_Not_Create_Patient_Under_18()
    {
        var user = new User("Maria Silva", "maria@email.com", TestData.ValidCpf);
        var eighteenTomorrow = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-PatientProfile.MinimumAge).AddDays(1);

        Assert.Throws<DomainException>(() =>
            user.CreatePatientProfile(eighteenTomorrow, BiologicalSex.Female, 165, Goal.WeightLoss));
    }

    [Fact]
    public void Should_Not_Update_Patient_To_Under_18()
    {
        var user = new User("Maria Silva", "maria@email.com", TestData.ValidCpf);
        var profile = user.CreatePatientProfile(DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-30), BiologicalSex.Female, 165, Goal.WeightLoss);
        var seventeenYearsAgo = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-17);

        Assert.Throws<DomainException>(() =>
            user.UpdatePatientProfile(seventeenYearsAgo, BiologicalSex.Female, 165, Goal.WeightLoss));
        Assert.NotEqual(seventeenYearsAgo, profile.BirthDate);
    }

    private static User CreatePatient()
    {
        var user = new User("Maria Silva", "maria@email.com", TestData.ValidCpf);
        user.CreatePatientProfile(new DateOnly(1995, 3, 10), BiologicalSex.Female, 165, Goal.Maintenance);
        return user;
    }

    [Fact]
    public void Should_Save_Routine()
    {
        var user = CreatePatient();

        user.UpdatePatientRoutine(new TimeOnly(6, 0), new TimeOnly(22, 30), new TimeOnly(18, 0));

        Assert.Equal(new TimeOnly(6, 0), user.PatientProfile!.WakeUpTime);
        Assert.Equal(new TimeOnly(22, 30), user.PatientProfile.BedTime);
        Assert.Equal(new TimeOnly(18, 0), user.PatientProfile.TrainingTime);
    }

    [Fact]
    public void Should_Accept_Night_Shift_Routine()
    {
        var user = CreatePatient();

        // Acorda às 14h e dorme às 6h: a janela atravessa a meia-noite.
        user.UpdatePatientRoutine(new TimeOnly(14, 0), new TimeOnly(6, 0), null);

        Assert.Equal(new TimeOnly(14, 0), user.PatientProfile!.WakeUpTime);
        Assert.Equal(new TimeOnly(6, 0), user.PatientProfile.BedTime);
    }

    [Fact]
    public void Should_Clear_Routine_With_Nulls()
    {
        var user = CreatePatient();
        user.UpdatePatientRoutine(new TimeOnly(6, 0), new TimeOnly(22, 0), new TimeOnly(18, 0));

        user.UpdatePatientRoutine(null, null, null);

        Assert.Null(user.PatientProfile!.WakeUpTime);
        Assert.Null(user.PatientProfile.BedTime);
        Assert.Null(user.PatientProfile.TrainingTime);
    }

    [Fact]
    public void Should_Not_Accept_Same_Wake_Up_And_Bed_Time()
    {
        var user = CreatePatient();

        Assert.Throws<DomainException>(() => user.UpdatePatientRoutine(new TimeOnly(7, 0), new TimeOnly(7, 0), null));
    }

    [Fact]
    public void Should_Not_Update_Routine_Without_Patient_Profile()
    {
        var user = new User("Maria Silva", "maria@email.com", TestData.ValidCpf);

        Assert.Throws<DomainException>(() => user.UpdatePatientRoutine(new TimeOnly(6, 0), null, null));
    }
}
