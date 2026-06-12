// GET /api/urls/{shortCode}
using UrlShortener.Core.Services;

namespace UrlShortener.Api.Endpoints;

public static class RedirectEndpoints
{
    public static void MapRedirectEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/urls/{shortCode}", async (
            string shortCode,
            UrlShortenerService service,
            HttpContext context) =>
        {
            var result = await service.ResolveAsync(shortCode);

            context.Response.Headers.CacheControl = "public, max-age=900, stale-while-revalidate=300";
            
            return result.IsFound
                ? Results.Redirect(result.LongUrl!, permanent: true)
                : Results.NotFound();
        });
    }
}