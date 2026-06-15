// POST /api/urls/shorten
using Microsoft.Extensions.Options;
using UrlShortener.Api.Configuration;
using UrlShortener.Contracts;
using UrlShortener.Core.Services;

namespace UrlShortener.Api.Endpoints;

public static class ShortenEndpoints
{
    public static void MapShortenEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/urls/shorten", async (
            ShortenRequest request,
            UrlShortenerService service,
            IOptions<AppSettings> appSettings) =>
        {
            try
            {
                var shortCode = await service.ShortenAsync(request.LongUrl);
                var baseUrl = appSettings.Value.BaseUrl.TrimEnd('/');
                return Results.Ok(new ShortenResponse($"{baseUrl}/{shortCode}"));
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        }).RequireRateLimiting(RateLimitConfiguration.ShortenPolicy); 
    }
}