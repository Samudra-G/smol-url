using Microsoft.EntityFrameworkCore;
using UrlShortener.Core.Entities;

namespace UrlShortener.Infrastructure.Persistence;


public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<ShortUrl> Urls => Set<ShortUrl>();
    public DbSet<UrlAnalyticsDaily> UrlAnalyticsDaily => Set<UrlAnalyticsDaily>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(u => u.Email).IsRequired();
            entity.HasIndex(u => u.Email).IsUnique();
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
    }
}