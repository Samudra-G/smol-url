using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using UrlShortener.Api.Configuration;
using UrlShortener.Contracts;
using UrlShortener.Core.Interfaces;
using UrlShortener.Core.Services;

namespace UrlShortener.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/auth");

        group.MapPost("/register", Register);
        group.MapPost("/login", Login);
        group.MapPost("/refresh", Refresh);
        group.MapGet("/google", GoogleChallenge);
        group.MapGet("/google/callback", GoogleCallback);
    }

    private static async Task<IResult> Register(
        RegisterRequest request,
        IAuthService auth)
    {
        try
        {
            var result = await auth.RegisterAsync(request);
            return Results.Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(new { error = ex.Message });
        }
    }

    private static async Task<IResult> Login(
        LoginRequest request,
        IAuthService auth)
    {
        var response = await auth.LoginAsync(request);
        return response is null ? Results.Unauthorized() : Results.Ok(response);
    }

    private static async Task<IResult> Refresh(
        RefreshTokenRequest request,
        IAuthService auth)
    {
        var response = await auth.RefreshTokenAsync(request);
        return response is null ? Results.Unauthorized() : Results.Ok(response);
    }

    private static IResult GoogleChallenge()
    {
        var props = new AuthenticationProperties { RedirectUri = "/auth/google/callback" };
        return Results.Challenge(props, ["Google"]);
    }

    private static async Task<IResult> GoogleCallback(
        HttpContext ctx,
        IAuthService auth,
        IOptions<AppSettings> settings,
        [FromQuery] string? returnUrl)
    {
        var result = await ctx.AuthenticateAsync("External");
        if(!result.Succeeded) return Results.Unauthorized();

        var allowedUrls = settings.Value.AllowedCallbackUrls;
        string targetRedirectUrl;
        if (string.IsNullOrWhiteSpace(returnUrl))
        {
            if (allowedUrls.Count == 0) return Results.BadRequest("No redirect URLs configured.");
            targetRedirectUrl = allowedUrls[0]; // Default fallback
        }
        else if (!allowedUrls.Contains(returnUrl))
        {
            return Results.BadRequest("The redirect URI provided is not authorized.");
        }
        else
        {
            targetRedirectUrl = returnUrl;
        }

        var googleId = result.Principal!.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var email = result.Principal!.FindFirstValue(ClaimTypes.Email)!;
        var name = result.Principal!.FindFirstValue(ClaimTypes.Name);

        var tokens = await auth.ProcessGoogleCallbackAsync(googleId, email, name);

        await ctx.SignOutAsync("External");

        return Results.Redirect($"{targetRedirectUrl}?access_token={tokens.AccessToken}&refresh_token={tokens.RefreshToken}");
    }
}