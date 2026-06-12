namespace UrlShortener.Core.Entities;

public class UrlAnalyticsDaily
{
    public long UrlId { get; set; }
    public DateOnly Date { get; set; }
    public long ClickCount { get; set; }
    
    public ShortUrl Url { get; set; } = null!;
}