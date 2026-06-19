using UrlShortener.Contracts;

namespace UrlShortener.Core.Interfaces;

public interface IAuthService
{
    Task<AuthResponse?> LoginAsync(LoginRequest request);
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse?> RefreshTokenAsync(RefreshTokenRequest request);
    Task<AuthResponse> ProcessGoogleCallbackAsync(string googleId, string email, string? name);
}