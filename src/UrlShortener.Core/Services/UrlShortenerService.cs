using UrlShortener.Core.Entities;
using UrlShortener.Core.Interfaces;

namespace UrlShortener.Core.Services;

/// <summary>
/// Provides domain-level business logic for generating short URLs and resolving short codes.
/// Coordinates transactional persistence, low-latency distributed caching, and high-throughput analytics tracking.
/// </summary>
public class UrlShortenerService
{
    private readonly IUrlRepository _repository;
    private readonly IUrlCache _cache;
    private readonly IAnalyticsRecorder _analytics;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="UrlShortenerService"/> class.
    /// </summary>
    /// <param name="repository">The relational data-store repository for absolute persistence.</param>
    /// <param name="cache">The distributed memory cache interface implementing standard cache-aside and negative caching policies.</param>
    /// <param name="analytics">The decoupled, non-blocking service for ingestion of redirection click telemetry.</param>
    public UrlShortenerService(
        IUrlRepository repository,
        IUrlCache cache,
        IAnalyticsRecorder analytics)
    {
        _repository = repository;
        _cache = cache;
        _analytics = analytics;
    }

    /// <summary>
    /// Validates, persists, and encodes a target long destination URL into a localized Base62 token string.
    /// </summary>
    /// <param name="longUrl">The target absolute HTTP or HTTPS URL string to shorten.</param>
    /// <param name="userId">The optional unique identifier of the system user creating the short record.</param>
    /// <param name="ct">The <see cref="CancellationToken"/> to monitor for operation cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation, containing the generated 7-character Base62 string code.</returns>
    /// <exception cref="ArgumentException">Thrown when the provided <paramref name="longUrl"/> is malformed, relative, or uses an unsupported protocol scheme.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown downstream if the underlying auto-incremented database identity value outgrows system capabilities.</exception>
    public async Task<string> ShortenAsync(
        string longUrl, Guid? userId = null, CancellationToken ct = default)
    {
        if (!Uri.TryCreate(longUrl, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new ArgumentException(
                "Long URL must be an absolute http or https URL.", nameof(longUrl));
        }

        var entity = new ShortUrl
        {
            LongUrl = longUrl,
            CreatedBy = userId
        };

        var created = await _repository.CreateAsync(entity, ct);
        return Base62Converter.Encode(created.Id);
    }

    /// <summary>
    /// Decodes and resolves a 7-character short token back to its destination URL.
    /// Utilizes a fast-fail decoder validation, short-circuiting cache hits, and cache-aside hydration on read misses.
    /// </summary>
    /// <param name="shortCode">The 7-character Base62 token passed from the public boundary mapping to an internal sequence ID.</param>
    /// <param name="ct">The <see cref="CancellationToken"/> to monitor for operation cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation, containing an expressive <see cref="UrlResolutionResult"/> state wrapper.</returns>
    public async Task<UrlResolutionResult> ResolveAsync(
        string shortCode, CancellationToken ct = default)
    {
        if(!Base62Converter.TryDecode(shortCode, out var id))
            return UrlResolutionResult.NotFound;

        var cached = await _cache.GetAsync(id);
        switch (cached.Status)
        {
            case CacheStatus.Hit:
                await _analytics.RecordClickAsync(id);
                return UrlResolutionResult.Found(cached.LongUrl!);

            case CacheStatus.NegativeHit:
                return UrlResolutionResult.NotFound;
        }

        var entity = await _repository.GetByIdAsync(id, ct);

        if(entity is null || !entity.IsActive || IsExpired(entity))
        {
            await _cache.SetNotFoundAsync(id);
            return UrlResolutionResult.NotFound;
        }

        await _cache.SetAsync(id, entity.LongUrl);
        await _analytics.RecordClickAsync(id);
        return UrlResolutionResult.Found(entity.LongUrl);
    }

    /// <summary>
    /// Ephemeral point-in-time check to verify if a short record has exceeded its expiration date payload.
    /// </summary>
    /// <param name="entity">The database entity containing timestamps.</param>
    /// <returns>True if the resource expiration timestamp is configured and has passed; otherwise, false.</returns>
    private static bool IsExpired(ShortUrl entity) =>
        entity.ExpiresAt is not null && entity.ExpiresAt <= DateTimeOffset.UtcNow;
}