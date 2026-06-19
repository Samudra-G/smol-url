using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;
using UrlShortener.Api.Configuration;
using UrlShortener.Api.Endpoints;
using UrlShortener.Core.Interfaces;
using UrlShortener.Core.Services;
using UrlShortener.Infrastructure.Auth;
using UrlShortener.Infrastructure.BackgroundServices;
using UrlShortener.Infrastructure.Caching;
using UrlShortener.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// ── Database ─────────────────────────────────────────────────────────────
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString).UseSnakeCaseNamingConvention());

// ── Redis ────────────────────────────────────────────────────────────────
var redisHost = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6380";
var redisOptions = ConfigurationOptions.Parse(redisHost);
redisOptions.AbortOnConnectFail = false;
redisOptions.ConnectRetry = 5;
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    ConnectionMultiplexer.Connect(redisOptions!));

// ── Typed options: app + url shortener ──────────────────────────────────
builder.Services
    .AddOptions<AppSettings>()
    .Bind(builder.Configuration.GetSection("AppSettings"))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// ── Core app services ────────────────────────────────────────────────────
builder.Services.AddScoped<IUrlRepository, UrlRepository>();
builder.Services.AddSingleton<IUrlCache, RedisUrlCache>();
builder.Services.AddSingleton<IAnalyticsRecorder, RedisAnalyticsRecorder>();
builder.Services.AddScoped<IAnalyticsRepository, AnalyticsRepository>();
builder.Services.AddScoped<UrlShortenerService>();
builder.Services.AddHostedService<AnalyticsFlushWorker>();
builder.Services.ConfigureRateLimiting();

// ── Auth: typed options ─────────────────────────────────────────────────
builder.Services
    .AddOptions<JwtSettings>()
    .Bind(builder.Configuration.GetSection("Jwt"))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// ── Auth: application services ──────────────────────────────────────────
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
builder.Services.AddScoped<IAuthService, AuthService>();

// ── Auth: schemes (JWT bearer + Google OAuth via temp cookie) ──────────
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer()  // options configured below via the options pipeline
    .AddCookie("External")
    .AddGoogle(options =>
    {
        options.ClientId     = builder.Configuration["Authentication:Google:ClientId"]!;
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]!;
        options.SignInScheme = "External";   // hands off to the temp cookie, not the JWT scheme
        options.CallbackPath = "/auth/google/callback";
        options.Scope.Add("email");
        options.Scope.Add("profile");
    });

// ── Auth: wire JwtBearer validation params from JwtSettings ────────────
builder.Services
    .AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<JwtSettings>>((bearerOptions, jwtSettings) =>
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtSettings.Value.SigningKey));

        bearerOptions.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey         = key,
            ValidateIssuer           = true,
            ValidIssuer              = jwtSettings.Value.Issuer,
            ValidateAudience         = true,
            ValidAudience            = jwtSettings.Value.Audience,
            ValidateLifetime         = true,
            ClockSkew                = TimeSpan.Zero,
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// ── Middleware pipeline ──────────────────────────────────────────────────
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

// ── Endpoints ─────────────────────────────────────────────────────────────
app.MapShortenEndpoints();
app.MapRedirectEndpoints();
app.MapAuthEndpoints();

app.Run();