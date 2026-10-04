namespace NogVita.Application.Abstractions;

public interface ISecureTokenService
{
    string GenerateToken();
    string Hash(string token);
}