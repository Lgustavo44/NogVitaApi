using NogVita.Domain.Common;
using NogVita.Domain.Users;

namespace NogVita.UnitTests.Domain.Users
{
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


    }
}
