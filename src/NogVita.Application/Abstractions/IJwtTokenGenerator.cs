using NogVita.Domain.Users;

namespace NogVita.Application.Abstractions;

public interface IJwtTokenGenerator
{
    AccessToken Generate(User user);
}