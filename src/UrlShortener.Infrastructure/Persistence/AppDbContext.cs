using Microsoft.EntityFrameworkCore;
using UrlShortener.Core.Entities;

namespace UrlShortener.Infrastructure.Persistence;


public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<ShortUrl> Urls => Set<ShortUrl>();
    public DbSet<UrlAnalyticsDaily> UrlAnalyticsDaily => Set<UrlAnalyticsDaily>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<UserExternalLogin> UserExternalLogins => Set<UserExternalLogin>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(u => u.Email).IsRequired();
            entity.Property(u => u.PasswordHash)
                  .HasMaxLength(255);
            entity.HasIndex(u => u.Email).IsUnique();
            entity.HasMany(u => u.RefreshTokens).WithOne(t => t.User);
            entity.HasMany(u => u.ExternalLogins).WithOne(e => e.User);
        });

        modelBuilder.Entity<ShortUrl>(entity =>
        {
            entity.ToTable("urls");
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Id)
                  .UseIdentityAlwaysColumn()
                  .HasIdentityOptions(startValue:1000000000);
            entity.Property(u => u.LongUrl).IsRequired();
            entity.Property(u => u.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(u => u.IsActive).HasDefaultValue(true);

            entity.HasOne(u => u.CreatedByUser)
                  .WithMany()
                  .HasForeignKey(u => u.CreatedBy)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<UrlAnalyticsDaily>(entity =>
        {
            entity.ToTable("url_analytics_daily");
            entity.HasKey(a => new { a.UrlId, a.Date });

            entity.HasOne(a => a.Url)
                  .WithMany()
                  .HasForeignKey(a => a.UrlId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("refresh_tokens");
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Id).UseIdentityAlwaysColumn();
            entity.Property(t => t.TokenHash).IsRequired().HasMaxLength(64);

            entity.HasOne(t => t.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserExternalLogin>(entity =>
        {
            entity.ToTable("user_external_logins");
            // Composite PK — one row per (provider, providerKey) combination
            entity.HasKey(e => new { e.Provider, e.ProviderKey });
            entity.Property(e => e.Provider).HasMaxLength(50);
            entity.Property(e => e.ProviderKey).HasMaxLength(255);

            entity.HasOne(e => e.User)
                .WithMany(u => u.ExternalLogins)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}