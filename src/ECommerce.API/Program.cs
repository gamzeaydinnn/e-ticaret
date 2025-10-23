using ECommerce.Data.Context;
using Microsoft.EntityFrameworkCore;
using ECommerce.Business.Services.Managers;
using ECommerce.Business.Services.Managers;
using ECommerce.Business.Services.Interfaces;
using WebPush;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Core.Interfaces;
using ECommerce.Data.Repositories;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using ECommerce.Entities.Concrete;
using ECommerce.Infrastructure.Services.BackgroundJobs;
// using Hangfire;
using ECommerce.Infrastructure.Services.MicroServices;
using Microsoft.AspNetCore.Authorization;
using ECommerce.Infrastructure.Services.Payment;
using ECommerce.Infrastructure.Config;
using ECommerce.Core.Constants;
using ECommerce.Infrastructure.Services.Email;
using ECommerce.Infrastructure.Services.FileStorage;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using ECommerce.API.Infrastructure;


// using ECommerce.Infrastructure.Services.BackgroundJobs;
// using Hangfire;
// using Hangfire.SqlServer;

var builder = WebApplication.CreateBuilder(args);

// CORS (ortama göre sıkılaştırma)
builder.Services.AddCors(options =>
{
    options.AddPolicy("Default", policy =>
    {
        var allowed = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
        if (allowed != null && allowed.Length > 0)
        {
            policy.WithOrigins(allowed)
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        }
        else
        {
            policy.WithOrigins("http://localhost:3000", "http://localhost:3001")
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        }
    });
});

// DbContext ekle - SQL Server (varsayılan) veya SQLite (dev) kullan
var useSqlite = builder.Configuration.GetValue<bool>("Database:UseSqlite");
builder.Services.AddDbContext<ECommerceDbContext>(options =>
{
    if (useSqlite)
    {
        var dbPath = builder.Configuration["Database:SqlitePath"] ?? "app.db";
        options.UseSqlite($"Data Source={dbPath}");
    }
    else
    {
        // Enable transient error resiliency for SQL Server connections to reduce 500s
        // caused by transient network errors (e.g. pre-login handshake failures).
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"), sqlOptions =>
        {
                // stronger retry policy: retry up to 8 times with a larger max delay (helps absorb transient pre-login handshake errors)
                sqlOptions.EnableRetryOnFailure(maxRetryCount: 8, maxRetryDelay: TimeSpan.FromSeconds(30), errorNumbersToAdd: null);
        });
    }
});

builder.Services
    .AddIdentityCore<User>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequireDigit = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.Password.RequiredLength = 6;
        options.SignIn.RequireConfirmedEmail = true;
    })
    .AddRoles<IdentityRole<int>>()
    .AddEntityFrameworkStores<ECommerceDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

// Hangfire - SQL Server kullan (Geçici olarak devre dışı - Azure bağlantı sorunu)
// builder.Services.AddHangfire(config => 
//     config.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
//           .UseSimpleAssemblyNameTypeSerializer()
//           .UseRecommendedSerializerSettings()
//           .UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection")));
// builder.Services.AddHangfireServer();

// Hangfire (tek seferde)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
/*builder.Services.AddHangfire(config => config.UseSqlServerStorage(connectionString));
builder.Services.AddHangfireServer();


var app = builder.Build();
app.UseHangfireDashboard();*/

// Recurring job (yeni API kullanımı)
// RecurringJob.AddOrUpdate<StockSyncJob>(
//     "stock-sync-job",
//     job => job.RunOnce(),
    // Cron.Hourly,
//     new RecurringJobOptions { TimeZone = TimeZoneInfo.Local }
// );

// JWT Auth
builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration[ConfigKeys.JwtIssuer],
            ValidAudience = builder.Configuration[ConfigKeys.JwtAudience],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration[ConfigKeys.JwtKey]))
        };
    });

// Bind settings
builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("AppSettings"));
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("AppSettings:EmailSettings"));
builder.Services.Configure<PaymentSettings>(builder.Configuration.GetSection("PaymentSettings"));
builder.Services.Configure<InventorySettings>(builder.Configuration.GetSection("Inventory"));

// Email + FileStorage services
builder.Services.AddSingleton<EmailSender>();
builder.Services.AddSingleton<IFileStorage>(sp =>
    new LocalFileStorage(builder.Environment.ContentRootPath));

// Repositories
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ICartRepository, CartRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IFavoriteRepository, FavoriteRepository>();
builder.Services.AddScoped<ICourierRepository, CourierRepository>();

// Services  
builder.Services.AddScoped<IUserService, UserManager>();
builder.Services.AddScoped<IProductService, ProductManager>();
builder.Services.AddScoped<IOrderService, OrderManager>();
builder.Services.AddScoped<ICartService, CartManager>();
// Payment provider selection by config
var paymentProvider = builder.Configuration["Payment:Provider"]?.ToLowerInvariant();
switch (paymentProvider)
{
    case "stripe":
        builder.Services.AddScoped<IPaymentService, StripePaymentService>();
        break;
    case "iyzico":
        builder.Services.AddScoped<IPaymentService, IyzicoPaymentService>();
        break;
    case "paypal":
        builder.Services.AddScoped<IPaymentService, PayPalPaymentService>();
        break;
    default:
        builder.Services.AddScoped<IPaymentService, PaymentManager>();
        break;
}
builder.Services.AddScoped<IShippingService, ShippingManager>();
builder.Services.AddScoped<ProductManager>();
builder.Services.AddScoped<OrderManager>();
builder.Services.AddScoped<UserManager>();
builder.Services.AddScoped<CartManager>();
builder.Services.AddScoped<InventoryManager>();
builder.Services.AddScoped<IInventoryService, InventoryManager>();
builder.Services.AddScoped<ECommerce.Business.Services.Interfaces.INotificationService, ECommerce.Business.Services.Managers.NotificationService>();
builder.Services.AddScoped<MicroSyncManager>();
builder.Services.AddScoped<IBannerService, BannerManager>();
builder.Services.AddScoped<IBrandRepository, BrandRepository>();
builder.Services.AddScoped<IDiscountRepository, DiscountRepository>();
builder.Services.AddScoped<IReviewRepository, ReviewRepository>();
builder.Services.AddScoped<IAddressRepository, AddressRepository>();
builder.Services.AddScoped<ICouponRepository, CouponRepository>();

builder.Services.AddScoped<IBannerRepository, ECommerce.Infrastructure.Services.BannerRepository>();


// services / managers
builder.Services.AddScoped<IBrandService, BrandManager>();
builder.Services.AddScoped<IDiscountService, DiscountManager>();
builder.Services.AddScoped<IReviewService, ReviewManager>();
builder.Services.AddScoped<IAddressService, AddressManager>();
builder.Services.AddScoped<ICouponService, CouponManager>();
builder.Services.AddScoped<ICategoryService, CategoryManager>();
builder.Services.AddScoped<IFavoriteService, FavoriteManager>();
builder.Services.AddScoped<ICourierService, CourierManager>();

// vs.

// Stock sync job as hosted service and injectable singleton
builder.Services.AddSingleton<StockSyncJob>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<StockSyncJob>());
// MicroService ve MicroSyncManager (HttpClient tabanlı)
builder.Services.AddHttpClient<IMicroService, ECommerce.Infrastructure.Services.MicroServices.MicroService>(client =>
{
    var baseUrl = builder.Configuration["MikroSettings:ApiUrl"];
    if (!string.IsNullOrWhiteSpace(baseUrl)) client.BaseAddress = new Uri(baseUrl);
}).SetHandlerLifetime(TimeSpan.FromMinutes(5));
builder.Services.AddScoped<MicroSyncManager>();
builder.Services.AddScoped<IAuthService, AuthManager>();

// // builder.Services.AddScoped<StockSyncJob>();

builder.Services.AddAuthorization();
// Add in-memory caching for read-heavy endpoints (prerender, products)
builder.Services.AddMemoryCache();

// CSRF protection (for cookie-based flows). For SPA using Authorization header this is not strictly
// necessary, but we expose a token endpoint for cases where a cookie+header double-submit is used.
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = "XSRF-TOKEN";
    options.Cookie.HttpOnly = false; // accessible to JS so SPA can read it if needed
    options.Cookie.SameSite = SameSiteMode.Strict;
});

// Rate Limiting (Global, IP-based). Values are read from configuration if present.
// Config keys (optional): RateLimiting:PermitLimit, RateLimiting:WindowSeconds, RateLimiting:QueueLimit
var rateCfg = builder.Configuration.GetSection("RateLimiting");
var permitLimit = rateCfg.GetValue<int?>("PermitLimit") ?? 100;
var windowSeconds = rateCfg.GetValue<int?>("WindowSeconds") ?? 60;
var queueLimit = rateCfg.GetValue<int?>("QueueLimit") ?? 0;

builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        // prefer X-Forwarded-For if present (behind proxy), fall back to remote IP
        var forwarded = httpContext.Request.Headers["X-Forwarded-For"].ToString();
        string? key = null;
        if (!string.IsNullOrWhiteSpace(forwarded))
        {
            var parts = forwarded.Split(',');
            key = parts.Length > 0 ? parts[0].Trim() : forwarded.Trim();
        }
        key ??= httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = permitLimit,
            Window = TimeSpan.FromSeconds(windowSeconds),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = queueLimit
        });
    });

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, ct) =>
    {
        try
        {
            if (!context.HttpContext.Response.HasStarted)
            {
                // Inform client how long to wait (seconds)
                context.HttpContext.Response.Headers.RetryAfter = ((int)TimeSpan.FromSeconds(windowSeconds).TotalSeconds).ToString();
                context.HttpContext.Response.ContentType = "application/json";
                var payload = System.Text.Json.JsonSerializer.Serialize(new { error = "Too many requests", retry_after_seconds = windowSeconds });
                await context.HttpContext.Response.WriteAsync(payload, ct);
            }
        }
        catch
        {
            // swallow to avoid throwing from rate limiter
        }
    };
});

// Prerender CI bypass token (optional): if you set Prerender:BypassToken in configuration or env,
// requests that include the same token in the X-Prerender-Token header or ?prerender_token=... will be
// exempt from the rate limiter (useful for controlled CI prerender runs). Keep this secret and
// restrict CI to use a stable IP when possible.
var prerenderBypassToken = builder.Configuration["Prerender:BypassToken"];

// Controller ekle (global input sanitization filter ekleniyor)
builder.Services.AddControllers(options =>
{
    options.Filters.Add(typeof(ECommerce.API.Infrastructure.SanitizeInputFilter));
});

// Push (Web Push) - dev-friendly VAPID keys. In production, set these via configuration/secrets.
var vapidSubject = builder.Configuration["Push:VapidSubject"] ?? "mailto:admin@example.com";
var vapidPublic = builder.Configuration["Push:VapidPublicKey"] ?? "<YOUR_PUBLIC_VAPID_KEY_PLACEHOLDER>";
var vapidPrivate = builder.Configuration["Push:VapidPrivateKey"] ?? "<YOUR_PRIVATE_VAPID_KEY_PLACEHOLDER>";
builder.Services.AddSingleton<IPushService>(sp => new PushService(sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<PushService>>(), vapidSubject, vapidPublic, vapidPrivate));

// Swagger (isteğe bağlı)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// app.UseHangfireDashboard();

// Recurring job (Geçici olarak devre dışı)
// // RecurringJob.AddOrUpdate<StockSyncJob>(
// //     job => job.RunOnce(), // StockSyncJob'da public async Task RunOnce() olmalı
//     // Cron.Hourly);

// DB init + Seed Roles/Admin User (ilk çalıştırmada)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var db = services.GetRequiredService<ECommerceDbContext>();
        // Dev/SQLite: şemayı oluştur; SQL Server: migrate/ensure
        try
        {
            var useSqliteAtRuntime = builder.Configuration.GetValue<bool>("Database:UseSqlite");
            if (useSqliteAtRuntime)
            {
                db.Database.EnsureCreated();
            }
            else
            {
                var pending = db.Database.GetPendingMigrations();
                if (pending != null && pending.Any())
                    db.Database.Migrate();
                else
                    db.Database.EnsureCreated();
            }
        }
        catch
        {
            // Yine de devam et
        }

        IdentitySeeder.SeedAsync(services).GetAwaiter().GetResult();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("Seed");
        logger.LogError(ex, "Identity seed sırasında hata oluştu");
    }
}

// Middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseHttpsRedirection();
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseCors("Default");
// Content Security Policy (per-request nonce available at HttpContext.Items["CSPNonce"]) 
app.UseMiddleware<ECommerce.API.Infrastructure.CspMiddleware>();
// Exempt some monitoring endpoints from rate limiting (health checks, metrics)
app.UseWhen(context =>
{
    var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;
    // exempt exactly /api/health and /metrics (and their subpaths)
    if (path.StartsWith("/api/health") || path.StartsWith("/metrics")) return false;
    // If a prerender bypass token is configured, only allow it to skip rate limiting for
    // prerender-related endpoints. This limits the blast radius of the secret.
    if (!string.IsNullOrWhiteSpace(prerenderBypassToken))
    {
        // Only consider bypass for dedicated prerender routes
        if (path.StartsWith("/api/prerender" ) || path.StartsWith("/api/prerender/list"))
        {
            var headerToken = context.Request.Headers["X-Prerender-Token"].ToString();
            var queryToken = context.Request.Query["prerender_token"].ToString();
            if (!string.IsNullOrWhiteSpace(headerToken) && headerToken == prerenderBypassToken) return false;
            if (!string.IsNullOrWhiteSpace(queryToken) && queryToken == prerenderBypassToken) return false;
        }
    }

    return true;
}, branch =>
{
    branch.UseRateLimiter();
});
app.UseAuthentication();
app.UseAuthorization();

// CSRF token endpoint (double-submit pattern for SPA)
app.MapGet("/api/csrf/token", (IAntiforgery antiforgery, HttpContext http) =>
{
    var tokens = antiforgery.GetAndStoreTokens(http);
    return Results.Ok(new { token = tokens.RequestToken });
});

// Centralized CSRF validation + logging middleware
app.UseMiddleware<ECommerce.API.Infrastructure.CsrfLoggingMiddleware>();

app.MapControllers();
app.Run();
