using UrlShortener.Core.Interfaces;

namespace UrlShortener.Tests.Fakes;

public class FakeUrlCache : IUrlCache
{
    // Seed these before a test to control what the cache returns
    private readonly Dictionary<long, CacheLookupResult> _store = new();

    public int GetCallCount { get; private set; }
    public int SetCallCount { get; private set; }
    public int SetNotFoundCallCount { get; private set; }

    // Track what was written to the cache
    public Dictionary<long, string> PositiveEntries { get; } = new();
    public HashSet<long> NegativeEntries { get; } = new();

    public void SeedHit(long id, string longUrl) =>
        _store[id] = CacheLookupResult.Found(longUrl);

    public void SeedNegativeHit(long id) =>
        _store[id] = CacheLookupResult.NotFound;

    public Task<CacheLookupResult> GetAsync(long id)
    {
        GetCallCount++;
        return Task.FromResult(
            _store.TryGetValue(id, out var result) ? result : CacheLookupResult.Miss);
    }

    public Task SetAsync(long id, string longUrl)
    {
        SetCallCount++;
        PositiveEntries[id] = longUrl;
        return Task.CompletedTask;
    }

    public Task SetNotFoundAsync(long id)
    {
        SetNotFoundCallCount++;
        NegativeEntries.Add(id);
        return Task.CompletedTask;
    }
}