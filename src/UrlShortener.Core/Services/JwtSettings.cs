using System.ComponentModel.DataAnnotations;

namespace UrlShortener.Core.Services;

public class JwtSettings
{
    [Required, MinLength(32)] public string SigningKey { get; set; } = string.Empty;
    [Required] public string Issuer { get; set; } = string.Empty;
    [Required] public string Audience { get; set; } = string.Empty;

    public int AccessTokenExpiryMinutes { get; set; } = 30;
    public int RefreshTokenExpiryDays { get; set; } = 7;

}