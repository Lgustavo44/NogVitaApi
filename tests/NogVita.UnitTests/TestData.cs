using NogVita.Domain.Users;

namespace NogVita.UnitTests;

public static class TestData
{
    public static readonly Cpf ValidCpf = Cpf.Create("12345678909");

    public static User CreateUser() => new("Maria Silva", "maria@email.com", ValidCpf);
}