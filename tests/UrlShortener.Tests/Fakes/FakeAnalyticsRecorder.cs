using UrlShortener.Core.Interfaces;

namespace UrlShortener.Tests.Fakes;

public class FakeAnalyticsRecorder : IAnalyticsRecorder
{
    public List<long> RecordedUrlIds { get; } = new();

    public Task RecordClickAsync(long urlId)
    {
        RecordedUrlIds.Add(urlId);
        return Task.CompletedTask;
    }
}