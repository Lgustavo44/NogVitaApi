using NogVita.Application.Auth;
using NogVita.Application.Patients;
using NogVita.Domain.Users;
using NogVita.UnitTests.Fakes;

namespace NogVita.UnitTests.Application.Patients;

public class RegisterPatientUseCaseTests
{
    private readonly FakeUserRepository _userRepository = new();
    private readonly FakePasswordHasher _passwordHasher = new();
    private readonly FakeRefreshTokenRepository _refreshTokenRepository = new();
    private readonly FakeUnitOfWork _unitOfWork = new();

    private static RegisterPatientRequest CreateRequest(string email = "joao@email.com", string cpf = "529.982.247-25") =>
        new("João Souza", email, cpf, "cafe-com-pao-de-queijo", new DateOnly(1995, 3, 10), BiologicalSex.Male, 178, Goal.MuscleGain);

    private RegisterPatientUseCase CreateUseCase()
    {
        var tokenIssuer = new TokenIssuer(
            new FakeJwtTokenGenerator(),
            new FakeSecureTokenService(),
            _refreshTokenRepository,
            new RefreshTokenSettings { ExpirationDays = 7 },
            TimeProvider.System);

        return new RegisterPatientUseCase(_userRepository, _passwordHasher, tokenIssuer, _unitOfWork);
    }

    [Fact]
    public async Task Should_Register_Active_Patient_And_Return_Tokens()
    {
        var useCase = CreateUseCase();

        var response = await useCase.ExecuteAsync(CreateRequest(), TestContext.Current.CancellationToken);

        Assert.NotNull(response);
        var user = Assert.Single(_userRepository.Users);
        Assert.True(user.IsActive);
        Assert.NotNull(user.PatientProfile);
        Assert.Equal("52998224725", user.Cpf.Value);
        Assert.Equal(_passwordHasher.Hash("cafe-com-pao-de-queijo"), user.PasswordHash);
        Assert.Contains(Roles.Patient, user.GetRoles());
        Assert.Single(_refreshTokenRepository.Tokens);
        Assert.Equal(1, _unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task Should_Return_Null_When_Email_Already_Exists()
    {
        _userRepository.Add(new User("Outra Pessoa", "joao@email.com", Cpf.Create("12345678909")));
        var useCase = CreateUseCase();

        var response = await useCase.ExecuteAsync(CreateRequest(email: "JOAO@email.com"), TestContext.Current.CancellationToken);

        Assert.Null(response);
        Assert.Single(_userRepository.Users);
        Assert.Equal(0, _unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task Should_Return_Null_When_Cpf_Already_Exists()
    {
        _userRepository.Add(new User("Outra Pessoa", "outra@email.com", Cpf.Create("52998224725")));
        var useCase = CreateUseCase();

        var response = await useCase.ExecuteAsync(CreateRequest(cpf: "529.982.247-25"), TestContext.Current.CancellationToken);

        Assert.Null(response);
        Assert.Single(_userRepository.Users);
        Assert.Equal(0, _unitOfWork.SaveChangesCount);
    }
}