namespace UrlShortener.Core.Interfaces;

public interface IAnalyticsRepository
{
    Task IncrementClickCountAsync(long urlId, DateOnly date, long delta, CancellationToken ct = default);
}
