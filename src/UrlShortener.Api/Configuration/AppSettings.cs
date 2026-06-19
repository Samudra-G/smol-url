namespace UrlShortener.Api.Configuration;

public class AppSettings
{
    public required string BaseUrl { get; set; }
    public List<string> AllowedCallbackUrls { get; set; } = [];
}