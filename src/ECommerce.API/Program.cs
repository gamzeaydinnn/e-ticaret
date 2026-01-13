using ECommerce.Data.Context;
using Microsoft.EntityFrameworkCore;
using ECommerce.Business.Services.Managers;
using ECommerce.Business.Services.Interfaces;
using WebPush;
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
using ECommerce.Infrastructure.Services;
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
using FluentValidation;
using FluentValidation.AspNetCore;
using ECommerce.API.Services.Sms;
using ECommerce.API.Validators;
using ECommerce.API.Services.Otp;
using ECommerce.Entities.Enums;
using ECommerce.API.Data;
using ECommerce.API.Authorization;


// using ECommerce.Infrastructure.Services.BackgroundJobs;
// using Hangfire;
// using Hangfire.SqlServer;

var builder = WebApplication.CreateBuilder(args);

// TEMPORARY: Disable ALL service validation for debugging DI issues
builder.Host.UseDefaultServiceProvider((context, options) =>
{
    options.ValidateScopes = false;
    options.ValidateOnBuild = false;
});

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
var sqlConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var forceSqlite = builder.Configuration.GetValue<bool>("Database:UseSqlite");
var useSqlite = forceSqlite || (string.IsNullOrWhiteSpace(sqlConnectionString) && builder.Environment.IsDevelopment());

if (!useSqlite && string.IsNullOrWhiteSpace(sqlConnectionString))
    throw new InvalidOperationException("ConnectionStrings:DefaultConnection is missing. Provide a SQL Server connection string or enable Database:UseSqlite for development.");
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
        options.UseSqlServer(sqlConnectionString, sqlOptions =>
        {
                // stronger retry policy: retry up to 8 times with a larger max delay (helps absorb transient pre-login handshake errors)
                sqlOptions.EnableRetryOnFailure(maxRetryCount: 8, maxRetryDelay: TimeSpan.FromSeconds(30), errorNumbersToAdd: null);
        });
    }
});

// Global LoggerService (ILogService)
builder.Services.AddScoped<ILogService, LoggerService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();

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

builder.Services.AddHttpContextAccessor();

// Hangfire - SQL Server kullan (Geçici olarak devre dışı - Azure bağlantı sorunu)
// builder.Services.AddHangfire(config => 
//     config.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
//           .UseSimpleAssemblyNameTypeSerializer()
//           .UseRecommendedSerializerSettings()
//           .UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection")));
// builder.Services.AddHangfireServer();

// Hangfire (tek seferde)
var connectionString = sqlConnectionString;
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
                Encoding.UTF8.GetBytes(builder.Configuration[ConfigKeys.JwtKey] ?? throw new InvalidOperationException("JwtKey is required")))
        };
        // Check revoked tokens (deny list) on token validation
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                var logger = ctx.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                var authHeader = ctx.Request.Headers["Authorization"].ToString();
                logger.LogInformation("🔵 JWT OnMessageReceived: Path={Path}, HasAuth={HasAuth}", 
                    ctx.Request.Path, !string.IsNullOrEmpty(authHeader));
                return Task.CompletedTask;
            },
            OnAuthenticationFailed = ctx =>
            {
                var logger = ctx.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogError("❌ JWT Auth Failed: {Message}, Exception={Exception}", 
                    ctx.Exception.Message, ctx.Exception.GetType().Name);
                if (ctx.Exception.InnerException != null)
                    logger.LogError("   Inner: {InnerMessage}", ctx.Exception.InnerException.Message);
                
                Console.WriteLine($"❌ JWT Auth Failed: {ctx.Exception.Message}");
                if (ctx.Exception.InnerException != null)
                    Console.WriteLine($"   Inner: {ctx.Exception.InnerException.Message}");
                return Task.CompletedTask;
            },
            OnTokenValidated = async ctx =>
            {
                var logger = ctx.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogInformation("✅ JWT Token Validated for user: {User}", 
                    ctx.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "unknown");
                
                try
                {
                    var denyList = ctx.HttpContext.RequestServices.GetService(typeof(ECommerce.Core.Interfaces.ITokenDenyList)) as ECommerce.Core.Interfaces.ITokenDenyList;
                    if (denyList == null) return;

                    var jti = ctx.Principal?.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti)?.Value;
                    if (string.IsNullOrWhiteSpace(jti)) return;

                    if (await denyList.IsDeniedAsync(jti))
                    {
                        logger.LogWarning("⚠️ Token revoked: jti={Jti}", jti);
                        ctx.Fail("Token revoked");
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error checking token deny list");
                    // swallow exceptions here to avoid breaking auth pipeline unexpectedly
                }
            }
        };
    });

// Bind settings
builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("AppSettings"));
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("AppSettings:EmailSettings"));
builder.Services.Configure<PaymentSettings>(builder.Configuration.GetSection("PaymentSettings"));
// Environment-based sandbox switch: in Development mode prefer sandbox endpoints
builder.Services.PostConfigure<PaymentSettings>(settings =>
{
    if (builder.Environment.IsDevelopment())
    {
        if (string.IsNullOrWhiteSpace(settings.IyzicoBaseUrl) || !settings.IyzicoBaseUrl.Contains("sandbox"))
            settings.IyzicoBaseUrl = "https://sandbox-api.iyzipay.com";
        // PayTR callback / keys could be set via appsettings.Development.json as needed
    }
    else
    {
        if (string.IsNullOrWhiteSpace(settings.IyzicoBaseUrl))
            settings.IyzicoBaseUrl = "https://api.iyzico.com/";
    }
});
builder.Services.Configure<InventorySettings>(builder.Configuration.GetSection("Inventory"));

// NetGSM SMS ve OTP servisleri
builder.Services.Configure<NetGsmSettings>(builder.Configuration.GetSection("NetGsm"));

// DEBUG: Configuration'ı dosyaya yaz
var netGsmSection = builder.Configuration.GetSection("NetGsm");
var debugPath = Path.Combine(builder.Environment.ContentRootPath, "netgsm_debug.txt");
File.WriteAllText(debugPath, $@"
=== DEBUG NETGSM CONFIGURATION ===
Section Exists: {netGsmSection.Exists()}
UserCode: '{netGsmSection["UserCode"]}'
Password: '{netGsmSection["Password"]}'
MsgHeader: '{netGsmSection["MsgHeader"]}'
AppName: '{netGsmSection["AppName"]}'
All Keys: {string.Join(", ", netGsmSection.GetChildren().Select(c => $"{c.Key}={c.Value}"))}
Environment: {builder.Environment.EnvironmentName}
=================================
");

// Eğer boşsa exception fırlat
if (string.IsNullOrWhiteSpace(netGsmSection["UserCode"]))
{
    var msg = $"FATAL: NetGsm:UserCode is empty! Check {debugPath} for details.";
    File.AppendAllText(debugPath, $"\nFATAL ERROR: {msg}\n");
    throw new InvalidOperationException(msg);
}

builder.Services.Configure<OtpSettings>(builder.Configuration.GetSection("Otp"));
builder.Services.Configure<SmsVerificationSettings>(
    builder.Configuration.GetSection("SmsVerification"));

// Typed HttpClient için NetGsmService'i doğru şekilde kaydet
builder.Services.AddHttpClient<NetGsmService>();

// Interface'ler için factory kullanarak aynı instance'ı kullan
builder.Services.AddScoped<INetGsmService>(sp => sp.GetRequiredService<NetGsmService>());
builder.Services.AddScoped<ISmsProvider>(sp => sp.GetRequiredService<NetGsmService>());

builder.Services.AddScoped<IOtpService, OtpService>();

// SMS Doğrulama Repository servisleri
builder.Services.AddScoped<ECommerce.Core.Interfaces.ISmsVerificationRepository, ECommerce.Data.Repositories.SmsVerificationRepository>();
builder.Services.AddScoped<ECommerce.Core.Interfaces.ISmsRateLimitRepository, ECommerce.Data.Repositories.SmsRateLimitRepository>();

// Email + FileStorage services
builder.Services.AddSingleton<EmailSender>();
builder.Services.AddSingleton<IFileStorage>(sp =>
    new LocalFileStorage(builder.Environment.ContentRootPath));
// Queues and background worker for messages
builder.Services.AddSingleton<ECommerce.Core.Messaging.MailQueue>();
builder.Services.AddSingleton<ECommerce.Core.Messaging.SmsQueue>();
builder.Services.AddHostedService<ECommerce.API.Services.MessageWorker>();

// Repositories
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ICartRepository, CartRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IFavoriteRepository, FavoriteRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<ICampaignRepository, CampaignRepository>();
builder.Services.AddScoped<ICourierRepository, CourierRepository>();
builder.Services.AddScoped<IWeightReportRepository, WeightReportRepository>();

// Services  
builder.Services.AddScoped<IUserService, UserManager>();
builder.Services.AddScoped<IProductService, ProductManager>();
builder.Services.AddScoped<IOrderService, OrderManager>();
builder.Services.AddScoped<ICartService, CartManager>();
builder.Services.AddScoped<IWeightService, WeightService>();
// Ödeme sağlayıcıları + PaymentManager (provider seçimi)
builder.Services.AddScoped<StripePaymentService>();
builder.Services.AddScoped<IyzicoPaymentService>();
builder.Services.AddScoped<PayPalPaymentService>();
builder.Services.AddScoped<PaymentManager>();
builder.Services.AddScoped<IPaymentService, PaymentManager>();
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
builder.Services.AddScoped<IPricingEngine, PricingEngine>();

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
builder.Services.AddScoped<ICampaignService, CampaignManager>();
builder.Services.AddScoped<IAdminLogService, LogManager>();
builder.Services.AddScoped<IInventoryLogService, InventoryLogService>();

// vs.

// Stock sync job as hosted service and injectable singleton
builder.Services.AddSingleton<StockSyncJob>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<StockSyncJob>());
builder.Services.AddHostedService<StockReservationCleanupJob>();
// Reconciliation job: daily reconciliation between provider reports and Payments table
builder.Services.AddSingleton<ECommerce.Infrastructure.Services.BackgroundJobs.ReconciliationJob>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<ECommerce.Infrastructure.Services.BackgroundJobs.ReconciliationJob>());
// MicroService ve MicroSyncManager (HttpClient tabanlı)
builder.Services.AddHttpClient<IMicroService, ECommerce.Infrastructure.Services.MicroServices.MicroService>(client =>
{
    var baseUrl = builder.Configuration["MikroSettings:ApiUrl"];
    if (!string.IsNullOrWhiteSpace(baseUrl)) client.BaseAddress = new Uri(baseUrl);
}).SetHandlerLifetime(TimeSpan.FromMinutes(5));
builder.Services.AddScoped<MicroSyncManager>();

// SMS Verification Service - Factory pattern ile tüm bağımlılıkları manuel çözümle
builder.Services.AddScoped<ECommerce.Business.Services.Interfaces.ISmsVerificationService>(serviceProvider =>
{
    // Bağımlılıkları manuel olarak al
    var verificationRepo = serviceProvider.GetRequiredService<ECommerce.Core.Interfaces.ISmsVerificationRepository>();
    var rateLimitRepo = serviceProvider.GetRequiredService<ECommerce.Core.Interfaces.ISmsRateLimitRepository>();
    var smsProvider = serviceProvider.GetRequiredService<ECommerce.Core.Interfaces.ISmsProvider>();
    var settings = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<SmsVerificationSettings>>();
    var logger = serviceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<SmsVerificationManager>>();
    
    return new SmsVerificationManager(verificationRepo, rateLimitRepo, smsProvider, settings, logger);
});

// AuthManager - Factory pattern ile SMS servisi dahil tüm bağımlılıkları çözümle
builder.Services.AddScoped<IAuthService>(sp =>
{
    var userManager = sp.GetRequiredService<UserManager<ECommerce.Entities.Concrete.User>>();
    var config = sp.GetRequiredService<IConfiguration>();
    var emailSender = sp.GetRequiredService<EmailSender>();
    var refreshTokenRepo = sp.GetRequiredService<IRefreshTokenRepository>();
    var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
    var smsService = sp.GetRequiredService<ECommerce.Business.Services.Interfaces.ISmsVerificationService>();
    var logger = sp.GetRequiredService<ILogger<AuthManager>>();
    
    return new AuthManager(userManager, config, emailSender, refreshTokenRepo, httpContextAccessor, smsService, logger);
});

// // builder.Services.AddScoped<StockSyncJob>();

// ================================================================================
// RBAC (Role-Based Access Control) Servisleri
// ================================================================================
// Permission ve RolePermission yönetimi için servisler
builder.Services.AddScoped<IPermissionService, PermissionManager>();
builder.Services.AddScoped<IRolePermissionService, RolePermissionManager>();

// Authorization altyapısı - dinamik permission policy'leri için
builder.Services.AddSingleton<IAuthorizationPolicyProvider, ECommerce.API.Authorization.PermissionPolicyProvider>();
builder.Services.AddScoped<IAuthorizationHandler, ECommerce.API.Authorization.PermissionAuthorizationHandler>();

// Authorization servisi - default fallback policy provider'ı korur
builder.Services.AddAuthorization();

// Add in-memory caching for read-heavy endpoints (prerender, products)
builder.Services.AddMemoryCache();
// Login rate-limit service (IMemoryCache-based)
builder.Services.AddSingleton<ECommerce.Business.Services.Interfaces.ILoginRateLimitService, ECommerce.Business.Services.Managers.LoginRateLimitService>();
// Token deny list (in-memory). Can be swapped with Redis implementation later.
builder.Services.AddSingleton<ECommerce.Core.Interfaces.ITokenDenyList, ECommerce.Infrastructure.Services.MemoryTokenDenyList>();

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

// FluentValidation registration
builder.Services.AddValidatorsFromAssemblyContaining<OrderCreateDtoValidator>();
builder.Services.AddFluentValidationAutoValidation();

// Push (Web Push) - dev-friendly VAPID keys. In production, set these via configuration/secrets.
var vapidSubject = builder.Configuration["Push:VapidSubject"] ?? "mailto:admin@example.com";
var vapidPublic = builder.Configuration["Push:VapidPublicKey"] ?? "<YOUR_PUBLIC_VAPID_KEY_PLACEHOLDER>";
var vapidPrivate = builder.Configuration["Push:VapidPrivateKey"] ?? "<YOUR_PRIVATE_VAPID_KEY_PLACEHOLDER>";
builder.Services.AddSingleton<IPushService>(sp => new PushService(sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<PushService>>(), vapidSubject, vapidPublic, vapidPrivate));
// SMS service (stub)
builder.Services.AddScoped<ECommerce.Business.Services.Interfaces.ISmsService, ECommerce.Business.Services.Managers.SmsService>();

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
Console.WriteLine("🚀🚀🚀 SEED BLOĞU BAŞLIYOR 🚀🚀🚀");
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    Console.WriteLine("✅ ServiceScope oluşturuldu");
    try
    {
        Console.WriteLine("🔍 DbContext alınıyor...");
        var db = services.GetRequiredService<ECommerceDbContext>();
        Console.WriteLine("✅ DbContext alındı");
        
        Console.WriteLine("🔍 Logger oluşturuluyor...");
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("Seed");
        Console.WriteLine("✅ Logger oluşturuldu");
        
        logger.LogInformation("🔍🔍🔍 Database initialization başlıyor...");
        Console.WriteLine("🔍🔍🔍 Database initialization başlıyor...");
        
        // Apply migrations (production-safe: works with existing databases)
        Console.WriteLine("🔍 Database.Migrate() çağrılıyor...");
        db.Database.Migrate();
        logger.LogInformation("✅ Database migrations uygulandı");
        Console.WriteLine("✅ Database migrations uygulandı");

        logger.LogInformation("🔍 IdentitySeeder başlatılıyor...");
        Console.WriteLine("🔍 IdentitySeeder başlatılıyor...");
        IdentitySeeder.SeedAsync(services).GetAwaiter().GetResult();
        logger.LogInformation("✅ IdentitySeeder tamamlandı");
        Console.WriteLine("✅ IdentitySeeder tamamlandı");
        
        logger.LogInformation("🔍 ProductSeeder başlatılıyor...");
        Console.WriteLine("🔍 ProductSeeder başlatılıyor...");
        ProductSeeder.SeedAsync(services).GetAwaiter().GetResult();
        logger.LogInformation("✅ ProductSeeder tamamlandı");
        Console.WriteLine("✅ ProductSeeder tamamlandı");
        
        // Banner seed - varsayılan ana sayfa görselleri
        logger.LogInformation("🖼️ BannerSeeder başlatılıyor...");
        Console.WriteLine("🖼️ BannerSeeder başlatılıyor...");
        BannerSeeder.SeedAsync(services).GetAwaiter().GetResult();
        logger.LogInformation("✅ BannerSeeder tamamlandı");
        Console.WriteLine("✅ BannerSeeder tamamlandı");
        
        // logger.LogInformation("🔍 CategorySeeder başlatılıyor...");
        // Console.WriteLine("🔍 CategorySeeder başlatılıyor...");
        // CategorySeeder.SeedAsync(db).GetAwaiter().GetResult();
        // logger.LogInformation("✅ CategorySeeder tamamlandı");
        // Console.WriteLine("✅ CategorySeeder tamamlandı");
        
        logger.LogInformation("✅ Tüm seed işlemleri başarıyla tamamlandı!");
        Console.WriteLine("✅✅✅ TÜM SEED İŞLEMLERİ BAŞARIYLA TAMAMLANDI! ✅✅✅");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌❌❌ SEED HATASI: {ex.Message}");
        Console.WriteLine($"❌ StackTrace: {ex.StackTrace}");
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("Seed");
        logger.LogError(ex, "❌ Database migration veya seed sırasında hata oluştu");
        throw; // Hatayı yeniden fırlat - uygulama başlamasın
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

// 📁 Uploads klasörü için static files desteği
// Banner/poster resimleri /uploads/... path'inden servis edilir
var uploadsPath = Path.Combine(builder.Environment.ContentRootPath, "uploads");
if (!Directory.Exists(uploadsPath))
{
    Directory.CreateDirectory(uploadsPath);
}

// Alt klasörleri de oluştur (banners, products, categories)
var bannersPath = Path.Combine(uploadsPath, "banners");
var productsPath = Path.Combine(uploadsPath, "products");
var categoriesPath = Path.Combine(uploadsPath, "categories");
if (!Directory.Exists(bannersPath)) Directory.CreateDirectory(bannersPath);
if (!Directory.Exists(productsPath)) Directory.CreateDirectory(productsPath);
if (!Directory.Exists(categoriesPath)) Directory.CreateDirectory(categoriesPath);

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads"
});

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

// Global exception handler (en sonda değil, controller'lardan önce)
app.UseMiddleware<ECommerce.API.Infrastructure.GlobalExceptionMiddleware>();

app.MapControllers();
app.Run();
