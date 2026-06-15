namespace UrlShortener.Core.Interfaces;

/// <summary>
/// Defines the data access contract for managing URL analytics and metrics.
/// </summary>
public interface IAnalyticsRepository
{
    /// <summary>
    /// Increments the daily click tracking count for a specific shortened URL.
    /// </summary>
    /// <param name="urlId">The unique identifier of the URL.</param>
    /// <param name="date">The calendar date for which the tracking metrics are recorded.</param>
    /// <param name="delta">The value by which to increment the click count (typically 1).</param>
    /// <param name="ct">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task IncrementClickCountAsync(long urlId, DateOnly date, long delta, CancellationToken ct = default);
}
