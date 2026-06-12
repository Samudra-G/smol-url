using StackExchange.Redis;
using UrlShortener.Core.Interfaces;

namespace UrlShortener.Infrastructure.Caching;

/// <summary>
/// Redis-backed implementation of the URL cache layer.
/// Implements high-throughput lookups using string key-value mappings.
/// </summary>
public class RedisUrlCache : IUrlCache
{
    private const string KeyPrefix = "url:";
    private const string NotFoundSentinel = "\0NOT_FOUND\0";

    private static readonly TimeSpan PositiveTtl = TimeSpan.FromHours(24);
    private static readonly TimeSpan NegativeTtl = TimeSpan.FromSeconds(60);

    private readonly IDatabase _db;

    public RedisUrlCache(IConnectionMultiplexer redis)
    {
        _db = redis.GetDatabase();
    }

    public async Task<CacheLookupResult> GetAsync(long id)
    {
        var value = await _db.StringGetAsync(GetKey(id));

        if (value.IsNullOrEmpty)
            return CacheLookupResult.Miss;
        if (value == NotFoundSentinel)
            return CacheLookupResult.NotFound;

        return CacheLookupResult.Found(value!);
    }

    public Task SetAsync(long id, string longUrl) =>
        _db.StringSetAsync(GetKey(id), longUrl, PositiveTtl);

    public Task SetNotFoundAsync(long id) =>
        _db.StringSetAsync(GetKey(id), NotFoundSentinel, NegativeTtl);

    private static string GetKey(long id) => $"{KeyPrefix}{id}";
}