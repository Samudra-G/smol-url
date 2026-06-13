using Microsoft.EntityFrameworkCore;
using UrlShortener.Core.Interfaces;

namespace UrlShortener.Infrastructure.Persistence;

public class AnalyticsRepository : IAnalyticsRepository
{
    private readonly AppDbContext _context;

    public AnalyticsRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task IncrementClickCountAsync(
        long urlId, DateOnly date, long delta, CancellationToken ct = default)
    {
        await _context.Database.ExecuteSqlInterpolatedAsync($@"
            INSERT INTO url_analytics_daily (url_id, date, click_count)
            VALUES ({urlId}, {date}, {delta})
            ON CONFLICT (url_id, date)
            DO UPDATE SET click_count = url_analytics_daily.click_count + EXCLUDED.click_count",
            ct);
    }
}