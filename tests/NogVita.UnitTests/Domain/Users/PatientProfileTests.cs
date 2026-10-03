using NogVita.Domain.Common;
using NogVita.Domain.Users;

namespace NogVita.UnitTests.Domain.Users
{
    public class PatientProfileTests
    {
        [Fact]
        public void Should_Not_Create_Patient_With_Future_BirthDate()
        {
            var user = new User("Maria Silva", "maria@email.com", "12345678909");
            var tomorrow = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);

            Assert.Throws<DomainException>(() =>
                user.CreatePatientProfile(tomorrow, BiologicalSex.Female, 165, Goal.WeightLoss));
        }
    }
}
