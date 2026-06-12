using Microsoft.EntityFrameworkCore;
using UrlShortener.Core.Entities;
using UrlShortener.Core.Interfaces;

namespace UrlShortener.Infrastructure.Persistence;

public class UrlRepository : IUrlRepository
{
    private readonly AppDbContext _context;

    public UrlRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ShortUrl> CreateAsync(ShortUrl shortUrl, CancellationToken ct = default)
    {
        _context.Urls.Add(shortUrl);
        await _context.SaveChangesAsync(ct);
        return shortUrl;
    }

    public async Task<ShortUrl?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        return await _context.Urls
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id, ct);
    }
}