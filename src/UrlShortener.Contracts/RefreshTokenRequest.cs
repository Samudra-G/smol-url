namespace UrlShortener.Contracts;

public record RefreshTokenRequest(string AccessToken, string RefreshToken);