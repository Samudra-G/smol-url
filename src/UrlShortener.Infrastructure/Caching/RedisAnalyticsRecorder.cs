using StackExchange.Redis;
using UrlShortener.Core.Interfaces;

namespace UrlShortener.Infrastructure.Caching;

/// <summary>
/// Redis-backed implementation for high-throughput analytics tracking.
/// Uses atomic counter incrementing and dirty-set tracking to enable decoupled background processing.
/// </summary>
public class RedisAnalyticsRecorder : IAnalyticsRecorder
{
    private const string ClickCounterPrefix = "clicks:";
    private const string DirtySetPrefix = "clicks:dirty:";

    private readonly IDatabase _db;
    public RedisAnalyticsRecorder(IConnectionMultiplexer redis)
    {
        _db = redis.GetDatabase();
    }

    public async Task RecordClickAsync(long urlId)
    {
        var date = DateOnly.FromDateTime(DateTime.UtcNow);
        var counterKey = GetCounterKey(date, urlId);
        var dirtySetKey = GetDirtySetKey(date);

        var batch = _db.CreateBatch();
        var incrTask = batch.StringIncrementAsync(counterKey);
        var addTask = batch.SetAddAsync(dirtySetKey, urlId);
        batch.Execute();

        await Task.WhenAll(incrTask, addTask);
    }

    /// <summary>
    /// Generates a namespaced key for the click metric storage string instance.
    /// Format result target: <c>clicks:yyyyMMdd:{id}</c>
    /// </summary>
    /// <param name="date">The transactional evaluation target date window.</param>
    /// <param name="urlId">The underlying 64-bit numerical tracking identity representing the short record.</param>
    /// <returns>A formatted string key indicating exact domain scope.</returns>
    internal static string GetCounterKey(DateOnly date, long urlId) =>
        $"{ClickCounterPrefix}{date:yyyyMMdd}:{urlId}";

    /// <summary>
    /// Generates a namespaced key for the tracking collection list tracking keys altered on the current date.
    /// Format result target: <c>clicks:dirty:yyyyMMdd</c>
    /// </summary>
    /// <param name="date">The transactional evaluation target date window.</param>
    /// <returns>A formatted string key indicating exact domain scope.</returns>
    internal static string GetDirtySetKey(DateOnly date) =>
        $"{DirtySetPrefix}{date:yyyyMMdd}";
}