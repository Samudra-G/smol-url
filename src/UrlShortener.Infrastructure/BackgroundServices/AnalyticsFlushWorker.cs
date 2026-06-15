using System.Diagnostics.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using UrlShortener.Core.Interfaces;
using UrlShortener.Infrastructure.Caching;

namespace UrlShortener.Infrastructure.BackgroundServices;

/// <summary>
/// A long-running background service that periodically flushes buffered URL click tracking analytics 
/// from Redis to the persistent relational database.
/// </summary>
/// <remarks>
/// This worker implements a write-behind caching pattern to reduce database write contention.
/// It uses a <see cref="IServiceScopeFactory"/> to safely consume the scoped <see cref="IAnalyticsRepository"/>
/// from within a hosted singleton lifetime.
/// </remarks>
public class AnalyticsFlushWorker : BackgroundService
{
    private static readonly TimeSpan FlushInterval = TimeSpan.FromSeconds(30);

    private readonly IConnectionMultiplexer _redis;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AnalyticsFlushWorker> _logger;

    public AnalyticsFlushWorker(
        IConnectionMultiplexer redis,
        IServiceScopeFactory scopeFactory,
        ILogger<AnalyticsFlushWorker> logger
    )
    {
        _redis = redis;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <summary>
    /// Core execution loop managed by the hosting environment.
    /// Runs continuously until the application shuts down or cancellation is requested.
    /// </summary>
    /// <param name="stoppingToken">Triggered when the host is shutting down or the service is stopping.</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous background operations.</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await FlushAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Analytics flush failed");
            }

            await Task.Delay(FlushInterval, stoppingToken);
        }
    }

    /// <summary>
    /// Initiates the flush cycle for both the current UTC calendar date and the previous day.
    /// </summary>
    /// <remarks>
    /// Flushing the previous day alongside the current day accounts for any trailing updates 
    /// delayed or recorded right around the midnight UTC boundary transition.
    /// </remarks>
    /// <param name="ct">A cancellation token to observe during processing.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous flush operation.</returns>
    private async Task FlushAsync(CancellationToken ct)
    {
        var db = _redis.GetDatabase();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        await FlushDateAsync(db, today, ct);
        await FlushDateAsync(db, today.AddDays(-1), ct);
    }

    /// <summary>
    /// Processes and persists the cached click analytics data for a specific date.
    /// </summary>
    /// <remarks>
    /// This method executes a resilient "persist-then-decrement" strategy to prevent data loss:
    /// <list type="number">
    /// <item>Fetches the current aggregate click count snapshot from Redis via <c>StringGetAsync</c>.</item>
    /// <item>Saves that snapshot to the primary database first using <see cref="IAnalyticsRepository"/>.</item>
    /// <item>Decrements the Redis key by the snapshotted amount via <c>StringDecrementAsync</c>.</item>
    /// </list>
    /// This relative subtraction pattern ensures that any concurrent click increments occurring mid-flush 
    /// are naturally retained in Redis rather than being lost.
    /// </remarks>
    /// <param name="db">The active Redis database instance.</param>
    /// <param name="date">The specific date to target for the flush sequence.</param>
    /// <param name="ct">A cancellation token to observe during processing.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    private async Task FlushDateAsync(IDatabase db, DateOnly date, CancellationToken ct)
    {
        var dirtySetKey = RedisAnalyticsRecorder.GetDirtySetKey(date);
        var urlIds = await db.SetMembersAsync(dirtySetKey);

        if(urlIds.Length == 0) return;

        using var scope = _scopeFactory.CreateScope();
        var analyticsRepository = scope.ServiceProvider.GetRequiredService<IAnalyticsRepository>();

        foreach (var urlIdValue in urlIds)
        {
            if(!long.TryParse((string)urlIdValue!, out var urlId)) continue;

            var counterKey = RedisAnalyticsRecorder.GetCounterKey(date, urlId);
            var snapshot = (long)await db.StringGetAsync(counterKey);
            if(snapshot <= 0)
            {
                await db.SetRemoveAsync(dirtySetKey, urlIdValue);
                continue;
            }

            await analyticsRepository.IncrementClickCountAsync(urlId, date, snapshot, ct);
            
            var remaining = await db.StringDecrementAsync(counterKey, snapshot);
            if (remaining <= 0)
            {
                await db.KeyDeleteAsync(counterKey);
                await db.SetRemoveAsync(dirtySetKey, urlIdValue);
            }

        }
    }
}