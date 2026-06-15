using UrlShortener.Core.Entities;
using UrlShortener.Core.Services;
using UrlShortener.Tests.Fakes;

namespace UrlShortener.Tests;

public class UrlShortenerServiceTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static (UrlShortenerService service,
                    FakeUrlRepository repo,
                    FakeUrlCache cache,
                    FakeAnalyticsRecorder analytics) BuildSut()
    {
        var repo = new FakeUrlRepository();
        var cache = new FakeUrlCache();
        var analytics = new FakeAnalyticsRecorder();
        var service = new UrlShortenerService(repo, cache, analytics);
        return (service, repo, cache, analytics);
    }

    private static ShortUrl ActiveUrl(long id, string longUrl) => new()
    {
        Id = id,
        LongUrl = longUrl,
        IsActive = true,
        CreatedAt = DateTimeOffset.UtcNow
    };

    // ── ShortenAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task ShortenAsync_ValidHttpUrl_ReturnsSevenCharCode()
    {
        var (sut, _, _, _) = BuildSut();

        var code = await sut.ShortenAsync("https://example.com");

        Assert.Equal(7, code.Length);
    }

    [Fact]
    public async Task ShortenAsync_ValidHttpUrl_ReturnsBase62EncodedId()
    {
        var (sut, repo, _, _) = BuildSut();

        var code = await sut.ShortenAsync("https://example.com");

        // The code should decode back to the ID the repo assigned
        var success = Base62Converter.TryDecode(code, out var decodedId);
        Assert.True(success);
        Assert.Equal(1_000_000_000L, decodedId); // matches FakeUrlRepository startId
    }

    [Fact]
    public async Task ShortenAsync_ValidHttpUrl_PersistsToRepository()
    {
        var (sut, repo, _, _) = BuildSut();

        await sut.ShortenAsync("https://example.com/path");

        var stored = await repo.GetByIdAsync(1_000_000_000L);
        Assert.NotNull(stored);
        Assert.Equal("https://example.com/path", stored.LongUrl);
    }

    [Theory]
    [InlineData("not-a-url")]
    [InlineData("ftp://example.com")]
    [InlineData("javascript:alert(1)")]
    [InlineData("")]
    public async Task ShortenAsync_InvalidUrl_ThrowsArgumentException(string input)
    {
        var (sut, _, _, _) = BuildSut();

        await Assert.ThrowsAsync<ArgumentException>(
            () => sut.ShortenAsync(input));
    }

    [Fact]
    public async Task ShortenAsync_HttpUrl_IsAccepted()
    {
        var (sut, _, _, _) = BuildSut();

        // http (not just https) should be valid
        var code = await sut.ShortenAsync("http://example.com");
        Assert.Equal(7, code.Length);
    }

    // ── ResolveAsync — cache hit ───────────────────────────────────────────────

    [Fact]
    public async Task ResolveAsync_CacheHit_ReturnsLongUrl()
    {
        var (sut, _, cache, _) = BuildSut();
        cache.SeedHit(1_000_000_000L, "https://example.com");
        var code = Base62Converter.Encode(1_000_000_000L);

        var result = await sut.ResolveAsync(code);

        Assert.True(result.IsFound);
        Assert.Equal("https://example.com", result.LongUrl);
    }

    [Fact]
    public async Task ResolveAsync_CacheHit_RecordsClick()
    {
        var (sut, _, cache, analytics) = BuildSut();
        cache.SeedHit(1_000_000_000L, "https://example.com");
        var code = Base62Converter.Encode(1_000_000_000L);

        await sut.ResolveAsync(code);

        Assert.Contains(1_000_000_000L, analytics.RecordedUrlIds);
    }

    [Fact]
    public async Task ResolveAsync_CacheHit_DoesNotHitRepository()
    {
        var (sut, repo, cache, _) = BuildSut();
        cache.SeedHit(1_000_000_000L, "https://example.com");
        var code = Base62Converter.Encode(1_000_000_000L);

        await sut.ResolveAsync(code);

        Assert.Equal(0, repo.GetByIdCallCount);
    }

    // ── ResolveAsync — negative cache hit ─────────────────────────────────────

    [Fact]
    public async Task ResolveAsync_NegativeCacheHit_ReturnsNotFound()
    {
        var (sut, _, cache, _) = BuildSut();
        cache.SeedNegativeHit(1_000_000_000L);
        var code = Base62Converter.Encode(1_000_000_000L);

        var result = await sut.ResolveAsync(code);

        Assert.False(result.IsFound);
    }

    [Fact]
    public async Task ResolveAsync_NegativeCacheHit_DoesNotRecordClick()
    {
        var (sut, _, cache, analytics) = BuildSut();
        cache.SeedNegativeHit(1_000_000_000L);
        var code = Base62Converter.Encode(1_000_000_000L);

        await sut.ResolveAsync(code);

        Assert.Empty(analytics.RecordedUrlIds);
    }

    // ── ResolveAsync — cache miss ──────────────────────────────────────────────

    [Fact]
    public async Task ResolveAsync_CacheMiss_ExistingUrl_ReturnsLongUrl()
    {
        var (sut, repo, _, _) = BuildSut();
        var url = ActiveUrl(1_000_000_000L, "https://example.com");
        await repo.CreateAsync(url);
        var code = Base62Converter.Encode(1_000_000_000L);

        var result = await sut.ResolveAsync(code);

        Assert.True(result.IsFound);
        Assert.Equal("https://example.com", result.LongUrl);
    }

    [Fact]
    public async Task ResolveAsync_CacheMiss_ExistingUrl_PopulatesCache()
    {
        var (sut, repo, cache, _) = BuildSut();
        var url = ActiveUrl(1_000_000_000L, "https://example.com");
        await repo.CreateAsync(url);
        var code = Base62Converter.Encode(1_000_000_000L);

        await sut.ResolveAsync(code);

        Assert.True(cache.PositiveEntries.ContainsKey(1_000_000_000L));
        Assert.Equal("https://example.com", cache.PositiveEntries[1_000_000_000L]);
    }

    [Fact]
    public async Task ResolveAsync_CacheMiss_ExistingUrl_RecordsClick()
    {
        var (sut, repo, _, analytics) = BuildSut();
        var url = ActiveUrl(1_000_000_000L, "https://example.com");
        await repo.CreateAsync(url);
        var code = Base62Converter.Encode(1_000_000_000L);

        await sut.ResolveAsync(code);

        Assert.Contains(1_000_000_000L, analytics.RecordedUrlIds);
    }

    [Fact]
    public async Task ResolveAsync_CacheMiss_NonExistentUrl_ReturnsNotFound()
    {
        var (sut, _, _, _) = BuildSut();
        var code = Base62Converter.Encode(1_000_000_000L); // nothing in repo

        var result = await sut.ResolveAsync(code);

        Assert.False(result.IsFound);
    }

    [Fact]
    public async Task ResolveAsync_CacheMiss_NonExistentUrl_SetsNegativeCache()
    {
        var (sut, _, cache, _) = BuildSut();
        var code = Base62Converter.Encode(1_000_000_000L);

        await sut.ResolveAsync(code);

        Assert.Contains(1_000_000_000L, cache.NegativeEntries);
    }

    // ── ResolveAsync — inactive / expired ─────────────────────────────────────

    [Fact]
    public async Task ResolveAsync_InactiveUrl_ReturnsNotFound()
    {
        var (sut, repo, _, _) = BuildSut();
        var url = new ShortUrl
        {
            Id = 1_000_000_000L,
            LongUrl = "https://example.com",
            IsActive = false,
            CreatedAt = DateTimeOffset.UtcNow
        };
        await repo.CreateAsync(url);
        var code = Base62Converter.Encode(1_000_000_000L);

        var result = await sut.ResolveAsync(code);

        Assert.False(result.IsFound);
    }

    [Fact]
    public async Task ResolveAsync_ExpiredUrl_ReturnsNotFound()
    {
        var (sut, repo, _, _) = BuildSut();
        var url = new ShortUrl
        {
            Id = 1_000_000_000L,
            LongUrl = "https://example.com",
            IsActive = true,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(-1), // expired an hour ago
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-1)
        };
        await repo.CreateAsync(url);
        var code = Base62Converter.Encode(1_000_000_000L);

        var result = await sut.ResolveAsync(code);

        Assert.False(result.IsFound);
    }

    [Fact]
    public async Task ResolveAsync_NotYetExpiredUrl_ReturnsFound()
    {
        var (sut, repo, _, _) = BuildSut();
        var url = new ShortUrl
        {
            Id = 1_000_000_000L,
            LongUrl = "https://example.com",
            IsActive = true,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1), // expires in the future
            CreatedAt = DateTimeOffset.UtcNow
        };
        await repo.CreateAsync(url);
        var code = Base62Converter.Encode(1_000_000_000L);

        var result = await sut.ResolveAsync(code);

        Assert.True(result.IsFound);
    }

    // ── ResolveAsync — invalid short codes ────────────────────────────────────

    [Fact]
    public async Task ResolveAsync_InvalidShortCode_ReturnsNotFound()
    {
        var (sut, _, _, _) = BuildSut();

        var result = await sut.ResolveAsync("!!!!!!!");

        Assert.False(result.IsFound);
    }

    [Fact]
    public async Task ResolveAsync_WrongLengthCode_ReturnsNotFound()
    {
        var (sut, _, _, _) = BuildSut();

        var result = await sut.ResolveAsync("abc");

        Assert.False(result.IsFound);
    }

    [Fact]
    public async Task ResolveAsync_InvalidCode_DoesNotHitRepository()
    {
        var (sut, repo, _, _) = BuildSut();

        await sut.ResolveAsync("!!!!!!!");

        Assert.Equal(0, repo.GetByIdCallCount);
    }
}