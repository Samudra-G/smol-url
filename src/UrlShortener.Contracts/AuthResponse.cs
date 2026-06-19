namespace UrlShortener.Contracts;

public record AuthResponse(string AccessToken, string RefreshToken, DateTimeOffset ExpiresAt);