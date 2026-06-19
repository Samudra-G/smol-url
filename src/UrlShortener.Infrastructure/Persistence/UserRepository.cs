using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using UrlShortener.Core.Entities;
using UrlShortener.Core.Interfaces;

namespace UrlShortener.Infrastructure.Persistence;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context) => _context = context;

    public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email, ct);

    public Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<User?> GetByExternalLoginAsync(
        string provider, string providerKey, CancellationToken ct = default) =>
        _context.UserExternalLogins
            .AsNoTracking()
            .Where(e => e.Provider == provider && e.ProviderKey == providerKey)
            .Select(e => e.User)
            .FirstOrDefaultAsync(ct);

    public async Task<User> CreateAsync(User user, CancellationToken ct = default)
    {
        _context.Users.Add(user);
        await _context.SaveChangesAsync(ct);
        return user;
    }

    public async Task AddExternalLoginAsync(UserExternalLogin login, CancellationToken ct = default)
    {
        _context.UserExternalLogins.Add(login);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<RefreshToken> CreateRefreshTokenAsync(
        RefreshToken token, CancellationToken ct = default)
    {
        _context.RefreshTokens.Add(token);
        await _context.SaveChangesAsync(ct);
        return token;
    }

    public Task<RefreshToken?> GetActiveRefreshTokenAsync(
        string tokenHash, CancellationToken ct = default) =>
        _context.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(
                t => t.TokenHash == tokenHash
                  && t.RevokedAt == null
                  && t.ExpiresAt > DateTimeOffset.UtcNow,
                ct);

    public async Task RevokeRefreshTokenAsync(RefreshToken token, CancellationToken ct = default)
    {
        token.RevokedAt = DateTimeOffset.UtcNow;
        _context.RefreshTokens.Update(token);
        await _context.SaveChangesAsync(ct);
    }
}