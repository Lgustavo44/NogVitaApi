using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using NogVita.Application.Abstractions;
using NogVita.Domain.Users;

namespace NogVita.Infrastructure.Security;

public sealed class JwtTokenGenerator(JwtSettings settings) : IJwtTokenGenerator
{
    public AccessToken Generate(User user)
    {
        var now = DateTime.UtcNow;
        var expiresAt = now.AddMinutes(settings.ExpirationMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        claims.AddRange(user.GetRoles().Select(role => new Claim("role", role)));

        var key = new SymmetricSecurityKey(Convert.FromBase64String(settings.SecretKey));

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = settings.Issuer,
            Audience = settings.Audience,
            Subject = new ClaimsIdentity(claims),
            IssuedAt = now,
            NotBefore = now,
            Expires = expiresAt,
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        };

        var token = new JsonWebTokenHandler().CreateToken(descriptor);

        return new AccessToken(token, expiresAt);
    }
}