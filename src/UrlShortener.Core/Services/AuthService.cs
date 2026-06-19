using System.Security.Claims;
using Microsoft.Extensions.Options;
using UrlShortener.Contracts;
using UrlShortener.Core.Entities;
using UrlShortener.Core.Interfaces;

namespace UrlShortener.Core.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepo;
    private readonly IJwtTokenService _jwtService;
    private readonly IPasswordHasher _hasher;
    private readonly JwtSettings _settings;

    public AuthService(
        IUserRepository userRepo,
        IJwtTokenService jwtService,
        IPasswordHasher hasher,
        IOptions<JwtSettings> settings)
    {
        _userRepo = userRepo;
        _jwtService = jwtService;
        _hasher = hasher;
        _settings = settings.Value;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        if(await _userRepo.GetByEmailAsync(request.Email) is not null)
            throw new InvalidOperationException("Email already registered");
        
        var user = new User
        {
            Email = request.Email,
            PasswordHash = _hasher.Hash(request.Password),
            DisplayName = request.DisplayName
        };

        await _userRepo.CreateAsync(user);
        return await IssueTokensAsync(user);
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request)
    {
        var user = await _userRepo.GetByEmailAsync(request.Email);
        if (user is null || 
            string.IsNullOrWhiteSpace(user.PasswordHash) || 
            string.IsNullOrWhiteSpace(request.Password))
        {
            _hasher.Verify("dummy_password", "dummy_hash_that_fails");
            return null;
        }

        var isValid = _hasher.Verify(request.Password, user.PasswordHash);
        if(!isValid) return null;

        return await IssueTokensAsync(user!);   
    }

    public async Task<AuthResponse?> RefreshTokenAsync(RefreshTokenRequest request)
    {
        var principal = await _jwtService.ValidateExpiredToken(request.AccessToken);
        if(principal is null) return null;

        var userIdClaim = principal.FindFirstValue(ClaimTypes.NameIdentifier) 
                       ?? principal.FindFirstValue("sub");
        if(!Guid.TryParse(userIdClaim, out var userId)) return null;

        var tokenHash = _hasher.HashToken(request.RefreshToken);
        var storedToken = await _userRepo.GetActiveRefreshTokenAsync(tokenHash);

        if(storedToken is null || storedToken.UserId != userId) return null;

        await _userRepo.RevokeRefreshTokenAsync(storedToken);
        return await IssueTokensAsync(storedToken.User);
    }

    public async Task<AuthResponse> ProcessGoogleCallbackAsync(string googleId, string email, string? name)
    {
        var user = await _userRepo.GetByExternalLoginAsync("Google", googleId);
        if(user is null)
        {
            user = await _userRepo.GetByEmailAsync(email);
            if(user is null)
            {
                user = await _userRepo.CreateAsync(new User
                {
                   Email = email,
                   PasswordHash = null,
                   DisplayName = name 
                });
            }

            await _userRepo.AddExternalLoginAsync(new UserExternalLogin
            {
                UserId = user.Id,
                Provider = "Google",
                ProviderKey = googleId,
            });
        }
        return await IssueTokensAsync(user);
    }

    // Private helper

    private async Task<AuthResponse> IssueTokensAsync(User user)
    {
        var rawRefreshToken = _jwtService.GenerateRefreshToken();
        var expiresAt = DateTimeOffset.UtcNow.AddDays(_settings.RefreshTokenExpiryDays);

        await _userRepo.CreateRefreshTokenAsync(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = _hasher.HashToken(rawRefreshToken),
            ExpiresAt = expiresAt
        });

        return new AuthResponse(
            AccessToken: _jwtService.GenerateAccessToken(user),
            RefreshToken: rawRefreshToken,
            ExpiresAt: expiresAt);
    }
}