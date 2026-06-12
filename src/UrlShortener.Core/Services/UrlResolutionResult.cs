namespace UrlShortener.Core.Services;

public readonly record struct UrlResolutionResult
{
    public bool IsFound { get; }
    public string? LongUrl { get; }

    private UrlResolutionResult(bool isFound, string? longUrl)
    {
        IsFound = isFound;
        LongUrl = longUrl;
    }

    public static UrlResolutionResult Found(string longUrl) => new(true, longUrl);
    public static readonly UrlResolutionResult NotFound = new(false, null);
}