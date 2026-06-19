using System.Security.Claims;
using UrlShortener.Core.Entities;

namespace UrlShortener.Core.Interfaces;

public interface IJwtTokenService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    Task<ClaimsPrincipal? >ValidateExpiredToken(string token);
}