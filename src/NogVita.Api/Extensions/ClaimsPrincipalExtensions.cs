using System.Security.Claims;

namespace NogVita.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var value = user.FindFirst("sub")?.Value;

        return Guid.TryParse(value, out var userId)
            ? userId
            : throw new InvalidOperationException("O token não contém um 'sub' válido.");
    }
}