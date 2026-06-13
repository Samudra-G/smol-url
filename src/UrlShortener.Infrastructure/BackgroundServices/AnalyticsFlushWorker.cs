using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using UrlShortener.Core.Interfaces;
using UrlShortener.Infrastructure.Caching;

namespace UrlShortener.Infrastructure.BackgroundServices;

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

    private async Task FlushAsync(CancellationToken ct)
    {
        var db = _redis.GetDatabase();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        await FlushDateAsync(db, today, ct);
        await FlushDateAsync(db, today.AddDays(-1), ct);
    }

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
            var deltaValue = await db.StringGetDeleteAsync(counterKey);

            if(deltaValue.TryParse(out long delta) && delta > 0)
            {
                await analyticsRepository.IncrementClickCountAsync(urlId, date, delta);
            }

            await db.SetRemoveAsync(dirtySetKey, urlIdValue);
        }
    }
}