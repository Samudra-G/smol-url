using UrlShortener.Core.Interfaces;

namespace UrlShortener.Infrastructure.Caching;

public class NoOpAnalyticsRecorder : IAnalyticsRecorder
{
    public Task RecordClickAsync(long urlId) => Task.CompletedTask;
}