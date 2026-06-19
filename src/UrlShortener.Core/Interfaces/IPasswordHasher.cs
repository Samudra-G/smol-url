namespace UrlShortener.Core.Interfaces;

public interface IPasswordHasher
{
    string Hash(string plaintext);
    bool Verify(string plaintext, string hash);
    string HashToken(string rawToken);
}