namespace UrlShortener.Core.Entities;

public class ShortUrl
{
    public long Id { get; set; }
    public string LongUrl { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;

    public Guid? CreatedBy { get; set; }
    public User? CreatedByUser { get; set; }
}