namespace NogVita.Application.Auth;

// D185: o "quem sou eu" tipado. O JSON é o mesmo de antes: { "userId": "...", "roles": ["Patient"] }.
public sealed record MeResponse(Guid UserId, IReadOnlyList<string> Roles);
