namespace UrlShortener.Core.Entities;

public class UserExternalLogin
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public string Provider { get; set; } = string.Empty;
    public string ProviderKey { get; set; } = string.Empty;
}