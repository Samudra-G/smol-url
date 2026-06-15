using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using UrlShortener.Api.Configuration;
using UrlShortener.Api.Endpoints;
using UrlShortener.Core.Interfaces;
using UrlShortener.Core.Services;
using UrlShortener.Infrastructure.BackgroundServices;
using UrlShortener.Infrastructure.Caching;
using UrlShortener.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString).UseSnakeCaseNamingConvention());

var redisHost = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6380";
var redisOptions = ConfigurationOptions.Parse(redisHost);
redisOptions.AbortOnConnectFail = false;
redisOptions.ConnectRetry = 5;
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    ConnectionMultiplexer.Connect(redisOptions!));

builder.Services
    .AddOptions<AppSettings>()
    .Bind(builder.Configuration.GetSection("AppSettings"))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddScoped<IUrlRepository, UrlRepository>();
builder.Services.AddSingleton<IUrlCache, RedisUrlCache>();
builder.Services.AddSingleton<IAnalyticsRecorder, RedisAnalyticsRecorder>();
builder.Services.AddScoped<IAnalyticsRepository, AnalyticsRepository>();
builder.Services.AddScoped<UrlShortenerService>();
builder.Services.AddHostedService<AnalyticsFlushWorker>();
builder.Services.ConfigureRateLimiting();

var app = builder.Build();

app.UseRateLimiter();

app.MapShortenEndpoints();
app.MapRedirectEndpoints();

app.Run();