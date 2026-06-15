using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace UrlShortener.Api.Configuration;

public static class RateLimitConfiguration
{
    public const string RedirectPolicy = "redirect_sliding_window";
    public const string ShortenPolicy = "shorten_concurrency";

    public static IServiceCollection ConfigureRateLimiting(
        this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddSlidingWindowLimiter(RedirectPolicy, opt =>
            {
                opt.PermitLimit = 100;
                opt.Window = TimeSpan.FromSeconds(10);
                opt.SegmentsPerWindow = 5;
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = 20;
            });

            options.AddConcurrencyLimiter(ShortenPolicy, opt =>
            {
                opt.PermitLimit = 10;
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = 5;
            });
        });

        return services;
    }
}