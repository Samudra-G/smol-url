using UrlShortener.Core.Entities;
using UrlShortener.Core.Interfaces;

namespace UrlShortener.Tests.Fakes;

public class FakeUrlRepository : IUrlRepository
{
    private long _nextId;
    private readonly Dictionary<long, ShortUrl> _store = new();

    public FakeUrlRepository(long startId = 1_000_000_000)
    {
        _nextId = startId;
    }

    public Task<ShortUrl> CreateAsync(ShortUrl shortUrl, CancellationToken ct = default)
    {
        CreateCallCount++;
        shortUrl.Id = _nextId++;
        shortUrl.CreatedAt = DateTimeOffset.UtcNow;
        _store[shortUrl.Id] = shortUrl;
        return Task.FromResult(shortUrl);
    }

    public Task<ShortUrl?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        GetByIdCallCount++;
        _store.TryGetValue(id, out var result);
        return Task.FromResult(result);
    }

    public int CreateCallCount { get; private set; }
    public int GetByIdCallCount { get; private set; }
}