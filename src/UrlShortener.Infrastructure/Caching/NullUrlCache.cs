using UrlShortener.Core.Interfaces;

namespace UrlShortener.Infrastructure.Caching;

/// <summary>
/// Temporary no-op cache — always reports a miss, writes go nowhere.
/// Replace with RedisUrlCache once Redis is wired up.
/// </summary>
public class NullUrlCache : IUrlCache
{
    public Task<CacheLookupResult> GetAsync(long id) =>
        Task.FromResult(CacheLookupResult.Miss);

    public Task SetAsync(long id, string longUrl) => Task.CompletedTask;

    public Task SetNotFoundAsync(long id) => Task.CompletedTask;
}