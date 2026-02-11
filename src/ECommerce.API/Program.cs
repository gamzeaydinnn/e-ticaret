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
using Hangfire;
using Hangfire.SqlServer;
using ECommerce.Infrastructure.Services.MicroServices;
using ECommerce.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using ECommerce.Infrastructure.Services.Payment;
using ECommerce.Infrastructure.Services.Payment.Posnet; // POSNET servisleri için
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
using ECommerce.API.Services; // RealTimeNotificationService için
using FluentValidation;
using FluentValidation.AspNetCore;
using ECommerce.API.Services.Sms;
using ECommerce.API.Validators;
using ECommerce.API.Services.Otp;
using ECommerce.Entities.Enums;
using ECommerce.API.Data;
using ECommerce.API.Authorization;
using ECommerce.API.Hubs; // SignalR Hub'ları için
using ECommerce.Core.Interfaces.Jobs; // Mikro Job interfaces

var builder = WebApplication.CreateBuilder(args);

// ═══════════════════════════════════════════════════════════════════════════════
// DI SERVICE VALIDATION
// Development'ta DI hatalarını erken yakalar, Production'da performans için kapalı
  // NEDEN: Yanlış scope'lu service bağımlılıkları runtime'da beklenmeyen hatalara yol açar
// ═══════════════════════════════════════════════════════════════════════════════
builder.Host.UseDefaultServiceProvider((context, options) =>
{
    // Development: Strict validation - DI hataları için
    if (context.HostingEnvironment.IsDevelopment())
    {
        options.ValidateScopes = true;
        options.ValidateOnBuild = true;
    }
    // Production: Performans için validation kapalı
    else
    {
        options.ValidateScopes = false;
        options.ValidateOnBuild = false;
    }
});

// CORS (ortama göre sıkılaştırma)
// SignalR için AllowCredentials gerekli
// GÜVENLİK: AllowAnyHeader/Method yerine explicit tanımlar
builder.Services.AddCors(options =>
{
    options.AddPolicy("Default", policy =>
    {
        var allowed = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
        if (allowed != null && allowed.Length > 0)
        {
            policy.WithOrigins(allowed)
                  .WithMethods("GET", "POST", "PUT", "DELETE", "PATCH", "OPTIONS")
                  .WithHeaders(
                      "Content-Type",
                      "Authorization",
                      "X-CSRF-TOKEN",
                      "X-Requested-With",
                      "Accept",
                      "Origin"
                  )
                  .AllowCredentials(); // SignalR için gerekli
        }
        else
        {
            policy.WithOrigins("http://localhost:3000", "http://localhost:3001")
                  .WithMethods("GET", "POST", "PUT", "DELETE", "PATCH", "OPTIONS")
                  .WithHeaders(
                      "Content-Type",
                      "Authorization",
                      "X-CSRF-TOKEN",
                      "X-Requested-With",
                      "Accept",
                      "Origin"
                  )
                  .AllowCredentials(); // SignalR için gerekli
        }
    });

    // SignalR özel CORS policy (WebSocket ve Long Polling desteği için)
    // NOT: SetIsOriginAllowed(_ => true) kaldırıldı - güvenlik açığı oluşturuyordu
    // WithOrigins zaten WebSocket bağlantılarını izin verilen origin'lerle kısıtlar
    options.AddPolicy("SignalR", policy =>
    {
        var allowed = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
        if (allowed != null && allowed.Length > 0)
        {
            policy.WithOrigins(allowed)
                  .WithMethods("GET", "POST", "OPTIONS")
                  .WithHeaders(
                      "Content-Type",
                      "Authorization",
                      "X-Requested-With",
                      "Accept",
                      "Origin"
                  )
                  .AllowCredentials();
        }
        else
        {
            policy.WithOrigins("http://localhost:3000", "http://localhost:3001")
                  .WithMethods("GET", "POST", "OPTIONS")
                  .WithHeaders(
                      "Content-Type",
                      "Authorization",
                      "X-Requested-With",
                      "Accept",
                      "Origin"
                  )
                  .AllowCredentials();
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
    
    // Suppress pending model changes warning (migrations are properly managed)
    options.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
});

// ═══════════════════════════════════════════════════════════════
// UNIT OF WORK PATTERN - Repository Coordination
// UnitOfWork pattern ile tüm repository'lerin merkezi yönetimi ve
// transaction koordinasyonu sağlanır
// ═══════════════════════════════════════════════════════════════
builder.Services.AddScoped<ECommerce.Core.Interfaces.IUnitOfWork, ECommerce.Data.Repositories.UnitOfWork>();

// DbContext'i generic olarak da ekle (bazı service'ler DbContext inject ediyor)
builder.Services.AddScoped<Microsoft.EntityFrameworkCore.DbContext>(provider =>
    provider.GetRequiredService<ECommerceDbContext>());

// Global LoggerService (ILogService)
builder.Services.AddScoped<ILogService, LoggerService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();

builder.Services
    .AddIdentityCore<User>(options =>
    {
        options.User.RequireUniqueEmail = true;

        // ═══════════════════════════════════════════════════════════
        // ŞİFRE POLİTİKASI - E-ticaret güvenlik standartları
        // NEDEN bu seviye: Ödeme ve kişisel veri içeren bir platformda
        // zayıf şifreler hesap ele geçirme riskini artırır
        // ═══════════════════════════════════════════════════════════
        options.Password.RequireDigit = true;            // En az 1 rakam (0-9)
        options.Password.RequireNonAlphanumeric = false;  // Özel karakter zorunlu değil (UX dengesı)
        options.Password.RequireUppercase = true;         // En az 1 büyük harf (A-Z)
        options.Password.RequireLowercase = true;         // En az 1 küçük harf (a-z)
        options.Password.RequiredLength = 8;              // Minimum 8 karakter

        options.SignIn.RequireConfirmedEmail = true;

        // Hesap kilitleme - brute force koruması
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.AllowedForNewUsers = true;
    })
    .AddRoles<IdentityRole<int>>()
    .AddEntityFrameworkStores<ECommerceDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddHttpContextAccessor();

// ═══════════════════════════════════════════════════════════════════════════════
// HANGFIRE - ZAMANLANMIŞ GÖREVLER
// Mikro ERP senkronizasyonu için recurring job'lar
// ═══════════════════════════════════════════════════════════════════════════════
var hangfireEnabled = builder.Configuration.GetValue<bool>("MikroApi:Jobs:HangfireEnabled", true);

if (hangfireEnabled)
{
    builder.Services.AddHangfire(config => config
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UseSqlServerStorage(sqlConnectionString, new SqlServerStorageOptions
        {
            CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
            SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
            QueuePollInterval = TimeSpan.FromSeconds(15),
            UseRecommendedIsolationLevel = true,
            DisableGlobalLocks = true,
            PrepareSchemaIfNecessary = true, // Tabloları otomatik oluştur
            SchemaName = "Hangfire" // Ayrı schema
        }));

    builder.Services.AddHangfireServer(options =>
    {
        options.WorkerCount = Environment.ProcessorCount * 2;
        options.Queues = new[] { "mikro-orders", "mikro-sync", "default" };
        options.ServerName = $"ECommerce-{Environment.MachineName}";
    });
}

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
                
                // GÜVENLİK: Önce httpOnly cookie'den token almayı dene
                // Bu, XSS saldırılarına karşı koruma sağlar
                if (string.IsNullOrEmpty(ctx.Token))
                {
                    var cookieToken = ctx.Request.Cookies["access_token"];
                    if (!string.IsNullOrEmpty(cookieToken))
                    {
                        ctx.Token = cookieToken;
                        logger.LogDebug("🔐 JWT: Token alındı httpOnly cookie'den");
                    }
                }
                
                // SignalR WebSocket bağlantıları için query string'den token al
                // WebSocket'ler HTTP header göndermediğinden token query param olarak gönderilir
                var path = ctx.Request.Path;
                if (path.StartsWithSegments("/hubs"))
                {
                    var accessToken = ctx.Request.Query["access_token"];
                    if (!string.IsNullOrEmpty(accessToken))
                    {
                        ctx.Token = accessToken;
                        logger.LogInformation("🔵 SignalR JWT: Token alındı query string'den, Path={Path}", path);
                    }
                }
                
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

// Site ayarları (Footer, İletişim bilgileri vb.)
builder.Services.Configure<ECommerce.Infrastructure.Config.SiteSettings>(
    builder.Configuration.GetSection("SiteSettings"));

// ==================== MİKRO ERP AYARLARI ====================
// MikroAPI V2 entegrasyonu için gerekli konfigürasyon
builder.Services.Configure<ECommerce.Infrastructure.Config.MikroSettings>(
    builder.Configuration.GetSection("MikroSettings"));

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

// NetGSM config kontrolü - boşsa mock SMS kullan
var netGsmSection = builder.Configuration.GetSection("NetGsm");
var useRealNetGsm = !string.IsNullOrWhiteSpace(netGsmSection["UserCode"])
                    && !string.IsNullOrWhiteSpace(netGsmSection["Password"])
                    && !string.IsNullOrWhiteSpace(netGsmSection["MsgHeader"]);

if (!useRealNetGsm)
{
    Console.WriteLine("[WARNING] NetGsm config boş - Mock SMS modu aktif.");
}

builder.Services.Configure<OtpSettings>(builder.Configuration.GetSection("Otp"));
builder.Services.Configure<SmsVerificationSettings>(
    builder.Configuration.GetSection("SmsVerification"));

// SMS Provider kayıt - config varsa NetGSM, yoksa Mock kullan
if (useRealNetGsm)
{
    // Typed HttpClient için NetGsmService'i doğru şekilde kaydet
    builder.Services.AddHttpClient<NetGsmService>();
    
    // Interface'ler için factory kullanarak aynı instance'ı kullan
    builder.Services.AddScoped<INetGsmService>(sp => sp.GetRequiredService<NetGsmService>());
    builder.Services.AddScoped<ISmsProvider>(sp => sp.GetRequiredService<NetGsmService>());
    
    Console.WriteLine("[INFO] NetGSM SMS servisi aktif");
}
else
{
    // Mock SMS Provider - Development/Test için
    builder.Services.AddScoped<ISmsProvider, ECommerce.API.Services.Sms.MockSmsProvider>();
    // INetGsmService için de mock döndür (null olmayacak şekilde)
    builder.Services.AddScoped<INetGsmService>(sp => 
        new MockNetGsmService(sp.GetRequiredService<ILogger<MockNetGsmService>>()));
    
    Console.WriteLine("[WARNING] Mock SMS servisi aktif - Gerçek SMS gönderilmeyecek!");
}

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
builder.Services.AddScoped<IWeightAdjustmentRepository, WeightAdjustmentRepository>();

// Services  
builder.Services.AddScoped<IUserService, UserManager>();
builder.Services.AddScoped<IProductService, ProductManager>();
builder.Services.AddScoped<IOrderService, OrderManager>();
builder.Services.AddScoped<ICartService, CartManager>();
builder.Services.AddScoped<IWeightService, WeightService>();
builder.Services.AddScoped<IWeightAdjustmentService, WeightAdjustmentService>();
builder.Services.AddScoped<IWeightBasedPaymentService, WeightBasedPaymentService>();
// Mikro ERP'den tartı verisi senkronizasyonu - sipariş teslim miktarlarını çeker
builder.Services.AddScoped<IMikroWeightSyncService, MikroWeightSyncService>();
// Ödeme sağlayıcıları + PaymentManager (provider seçimi)
// Stripe / PayPal devre dışı: sadece Iyzico + Posnet kullanılıyor
builder.Services.AddScoped<IyzicoPaymentService>();

// ═══════════════════════════════════════════════════════════════════════════════════════════════
// POSNET / YAPI KREDİ ÖDEME SİSTEMİ
// ═══════════════════════════════════════════════════════════════════════════════════════════════
// Yapı Kredi POSNET XML API entegrasyonu için gerekli servisler
// Aktif etmek için appsettings.json'da "Payment:Posnet:Enabled": true olmalı
// ═══════════════════════════════════════════════════════════════════════════════════════════════
var posnetEnabled = builder.Configuration.GetValue<bool>("Payment:Posnet:Enabled");
if (posnetEnabled)
{
    // POSNET servislerini kaydet
    builder.Services.AddPosnetPaymentServices();
    
    builder.Services.AddLogging(logging =>
    {
        logging.AddDebug();
    });
}

// PaymentManager (tüm provider'ları yönetir)
builder.Services.AddScoped<PaymentManager>();
builder.Services.AddScoped<IPaymentService, PaymentManager>();
builder.Services.AddScoped<IExtendedPaymentService, PaymentManager>();
builder.Services.AddScoped<IShippingService, ShippingManager>();
builder.Services.AddScoped<ICartSettingsService, CartSettingsManager>();
builder.Services.AddScoped<ProductManager>();
builder.Services.AddScoped<OrderManager>();
builder.Services.AddScoped<UserManager>();
builder.Services.AddScoped<CartManager>();
builder.Services.AddScoped<InventoryManager>();
builder.Services.AddScoped<IInventoryService, InventoryManager>();
builder.Services.AddScoped<ECommerce.Business.Services.Interfaces.INotificationService, ECommerce.Business.Services.Managers.NotificationService>();

// OrderStateMachine - Sipariş durum geçişlerini yönetir
builder.Services.AddScoped<IOrderStateMachine, OrderStateMachine>();

// RefundManager - İade talebi yönetim servisi
builder.Services.AddScoped<IRefundService, RefundManager>();

// PaymentCaptureService - Authorize/Capture ödeme akışını yönetir
builder.Services.AddScoped<IPaymentCaptureService, PaymentCaptureService>();

// WebhookValidationService - Webhook güvenlik doğrulama (HMAC, timestamp, idempotency)
builder.Services.AddScoped<IWebhookValidationService, WebhookValidationService>();

// CourierAuthService - Kurye authentication işlemleri (login, logout, refresh token)
builder.Services.AddScoped<ICourierAuthService, CourierAuthManager>();

// CourierOrderService - Kurye sipariş işlemleri (listeleme, teslimat, problem bildirimi)
builder.Services.AddScoped<ICourierOrderService, CourierOrderManager>();

builder.Services.AddScoped<MicroSyncManager>();
builder.Services.AddScoped<IBannerService, BannerManager>();
builder.Services.AddScoped<IBrandRepository, BrandRepository>();
builder.Services.AddScoped<IDiscountRepository, DiscountRepository>();
builder.Services.AddScoped<IReviewRepository, ReviewRepository>();
builder.Services.AddScoped<IAddressRepository, AddressRepository>();
builder.Services.AddScoped<ICouponRepository, CouponRepository>();
builder.Services.AddScoped<IPricingEngine, PricingEngine>();

builder.Services.AddScoped<IBannerRepository, ECommerce.Infrastructure.Services.BannerRepository>();

// ═══════════════════════════════════════════════════════════════════════════════
// ANA SAYFA ÜRÜN BLOK SİSTEMİ
// Admin panelinden yönetilebilir ürün blokları (İndirimli Ürünler, Süt Ürünleri vb.)
// ═══════════════════════════════════════════════════════════════════════════════
builder.Services.AddScoped<IHomeBlockRepository, HomeBlockRepository>();
builder.Services.AddScoped<IHomeBlockService, HomeBlockManager>();


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
builder.Services.AddScoped<ICampaignApplicationService, CampaignApplicationService>(); // Kampanya Uygulama Motoru
builder.Services.AddScoped<IAdminLogService, LogManager>();
builder.Services.AddScoped<IInventoryLogService, InventoryLogService>();

// =============================================
// Newsletter (Bülten) Sistemi
// Email abonelik yönetimi ve toplu mail gönderimi
// =============================================
builder.Services.AddScoped<INewsletterService, NewsletterManager>();

// =============================================
// XML/Variant Sistemi - Repository ve Service Kayıtları
// =============================================

// ProductVariant & Option Repositories
builder.Services.AddScoped<IProductVariantRepository, ProductVariantRepository>();
builder.Services.AddScoped<IProductOptionRepository, ProductOptionRepository>();
builder.Services.AddScoped<IXmlFeedSourceRepository, XmlFeedSourceRepository>();

// ProductVariant & Option Services
builder.Services.AddScoped<IProductVariantService, ProductVariantManager>();
builder.Services.AddScoped<IProductOptionService, ProductOptionManager>();

// XML Feed & Import Services  
builder.Services.AddScoped<IXmlFeedSourceService, XmlFeedSourceManager>();
builder.Services.AddScoped<IXmlImportService, XmlImportManager>();

// =============================================

// vs.

// Stock sync job as hosted service and injectable singleton
builder.Services.AddSingleton<StockSyncJob>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<StockSyncJob>());
builder.Services.AddHostedService<StockReservationCleanupJob>();
// Reconciliation job: daily reconciliation between provider reports and Payments table
builder.Services.AddSingleton<ECommerce.Infrastructure.Services.BackgroundJobs.ReconciliationJob>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<ECommerce.Infrastructure.Services.BackgroundJobs.ReconciliationJob>());
// MicroService ve MicroSyncManager (HttpClient tabanlı)
// SSL sertifika doğrulaması - sadece Development'ta bypass edilir
builder.Services.AddHttpClient<IMicroService, ECommerce.Infrastructure.Services.MicroServices.MicroService>(client =>
{
    var baseUrl = builder.Configuration["MikroSettings:ApiUrl"];
    if (!string.IsNullOrWhiteSpace(baseUrl)) client.BaseAddress = new Uri(baseUrl);
})
.ConfigurePrimaryHttpMessageHandler(() =>
{
    var handler = new HttpClientHandler();

    // ═══════════════════════════════════════════════════════════
    // SSL CERTIFICATE VALIDATION
    // Development: Self-signed certificate'lar için bypass
    // Production: Gerçek certificate validation (MITM koruması)
    // ═══════════════════════════════════════════════════════════
    if (builder.Environment.IsDevelopment())
    {
        // Dev: Self-signed sertifika kabul et
        handler.ServerCertificateCustomValidationCallback =
            HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
    }
    // Production: Varsayılan certificate validation kullanılır (güvenli)

    return handler;
})
.SetHandlerLifetime(TimeSpan.FromMinutes(5));

// MicroService'i concrete type olarak da kaydet (AdminMicroController için)
builder.Services.AddHttpClient<ECommerce.Infrastructure.Services.MicroServices.MicroService>(client =>
{
    var baseUrl = builder.Configuration["MikroSettings:ApiUrl"];
    if (!string.IsNullOrWhiteSpace(baseUrl)) client.BaseAddress = new Uri(baseUrl);
})
.ConfigurePrimaryHttpMessageHandler(() =>
{
    var handler = new HttpClientHandler();
    if (builder.Environment.IsDevelopment())
    {
        handler.ServerCertificateCustomValidationCallback =
            HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
    }
    return handler;
})
.SetHandlerLifetime(TimeSpan.FromMinutes(5));

builder.Services.AddScoped<MicroSyncManager>();

// ================================================================================
// MikroAPI V2 Çift Yönlü Senkronizasyon Servisleri
// ================================================================================
// NEDEN: Mağaza + online siparişlerin ERP ile tam entegrasyonu için
// Delta sync desteği, retry mekanizması, detaylı loglama

// Repository - sync state ve log yönetimi
builder.Services.AddScoped<ECommerce.Core.Interfaces.Sync.IMikroSyncRepository, 
    ECommerce.Data.Repositories.MikroSyncRepository>();

// Sync Servisleri - her biri tek sorumluluk (SRP)
builder.Services.AddScoped<ECommerce.Core.Interfaces.Sync.IStokSyncService, 
    ECommerce.Business.Services.Sync.StokSyncService>();
builder.Services.AddScoped<ECommerce.Core.Interfaces.Sync.ISiparisSyncService, 
    ECommerce.Business.Services.Sync.SiparisSyncService>();
builder.Services.AddScoped<ECommerce.Core.Interfaces.Sync.ICariSyncService, 
    ECommerce.Business.Services.Sync.CariSyncService>();
builder.Services.AddScoped<ECommerce.Core.Interfaces.Sync.IFiyatSyncService, 
    ECommerce.Business.Services.Sync.FiyatSyncService>();
// Fatura Sync - sipariş sonrası fatura kesimi
builder.Services.AddScoped<ECommerce.Core.Interfaces.Sync.IFaturaSyncService, 
    ECommerce.Business.Services.Sync.FaturaSyncService>();

// Orchestrator - tüm sync'leri koordine eder
builder.Services.AddScoped<ECommerce.Core.Interfaces.Sync.IMikroSyncService, 
    ECommerce.Business.Services.Sync.MikroSyncOrchestrator>();

// Mikro ERP Mapper Servisleri - entity dönüşümleri
builder.Services.AddScoped<ECommerce.Core.Interfaces.Mapping.IMikroStokMapper, 
    ECommerce.Business.Services.Mapping.MikroStokMapper>();
builder.Services.AddScoped<ECommerce.Core.Interfaces.Mapping.IMikroSiparisMapper, 
    ECommerce.Business.Services.Mapping.MikroSiparisMapper>();
builder.Services.AddScoped<ECommerce.Core.Interfaces.Mapping.IMikroCariMapper, 
    ECommerce.Business.Services.Mapping.MikroCariMapper>();
builder.Services.AddScoped<ECommerce.Core.Interfaces.Mapping.IMikroCategoryMappingService, 
    ECommerce.Business.Services.Mapping.CategoryMappingService>();

// ═══════════════════════════════════════════════════════════════════════════════
// ÇAKIŞMA YÖNETİMİ VE LOGLAMA SERVİSLERİ - ADIM 6
// ═══════════════════════════════════════════════════════════════════════════════
// Sync Logger - Tüm sync işlemlerini loglar
builder.Services.AddScoped<ECommerce.Core.Interfaces.Sync.ISyncLogger, 
    ECommerce.Business.Services.Sync.SyncLogger>();

// Conflict Resolvers - Çakışma çözümleyiciler
builder.Services.AddScoped<ECommerce.Core.Interfaces.Sync.IConflictResolver, 
    ECommerce.Business.Services.Sync.StockConflictResolver>();
builder.Services.AddScoped<ECommerce.Business.Services.Sync.PriceConflictResolver>();

// Retry Service - Başarısız işlemleri yeniden dener
builder.Services.AddScoped<ECommerce.Core.Interfaces.Sync.IRetryService, 
    ECommerce.Business.Services.Sync.RetryService>();

// ═══════════════════════════════════════════════════════════════════════════════
// HANGFIRE JOB SERVİSLERİ - Zamanlanmış Mikro Senkronizasyon Görevleri
// ═══════════════════════════════════════════════════════════════════════════════
builder.Services.AddScoped<ECommerce.Core.Interfaces.Jobs.IStokSyncJob, 
    ECommerce.Business.Services.Jobs.MikroStokSyncJob>();
builder.Services.AddScoped<ECommerce.Core.Interfaces.Jobs.IFiyatSyncJob, 
    ECommerce.Business.Services.Jobs.MikroFiyatSyncJob>();
builder.Services.AddScoped<ECommerce.Core.Interfaces.Jobs.IFullSyncJob, 
    ECommerce.Business.Services.Jobs.MikroFullSyncJob>();
builder.Services.AddScoped<ECommerce.Core.Interfaces.Jobs.ISiparisPushJob, 
    ECommerce.Business.Services.Jobs.SiparisPushJob>();
builder.Services.AddScoped<ECommerce.Business.Services.Jobs.RetryJob>();
builder.Services.AddScoped<ECommerce.Core.Interfaces.Jobs.IMikroJobScheduler, 
    ECommerce.Business.Services.Jobs.MikroJobScheduler>();

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

// =============================================
// SignalR Gerçek Zamanlı Bildirim Servisi
// Order tracking, Admin notifications, Courier updates
// =============================================
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
    options.MaximumReceiveMessageSize = 32 * 1024; // 32 KB - güvenlik için sınırlı
});

// RealTimeNotificationService - Hub'lara bildirim göndermek için merkezi servis
builder.Services.AddScoped<IRealTimeNotificationService, ECommerce.API.Services.RealTimeNotificationService>();

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

// Controller ekle
// NOT: SanitizeInputFilter kaldırıldı - HTML encoding Türkçe karakterleri bozuyordu (ş→&#x15F;)
// XSS koruması output'ta yapılmalı, React zaten bunu otomatik yapıyor (JSX output escaping)
// Input validation için FluentValidation kullanılıyor
// JSON Serialization: camelCase kullanılıyor (frontend uyumluluğu için)
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // camelCase property isimleri (örn: ProductName → productName)
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        // Büyük/küçük harf duyarsız okuma
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
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

        logger.LogInformation("🔍 IdentitySeeder başlatılıyor (sadece DB boşsa çalışır)...");
        Console.WriteLine("🔍 IdentitySeeder başlatılıyor (sadece DB boşsa çalışır)...");
        IdentitySeeder.SeedAsync(services).GetAwaiter().GetResult();
        logger.LogInformation("✅ IdentitySeeder tamamlandı");
        Console.WriteLine("✅ IdentitySeeder tamamlandı");
        
        logger.LogInformation("🔍 ProductSeeder başlatılıyor (sadece DB boşsa çalışır)...");
        Console.WriteLine("🔍 ProductSeeder başlatılıyor (sadece DB boşsa çalışır)...");
        ProductSeeder.SeedAsync(services).GetAwaiter().GetResult();
        logger.LogInformation("✅ ProductSeeder tamamlandı");
        Console.WriteLine("✅ ProductSeeder tamamlandı");
        
        // Banner seed - varsayılan ana sayfa görselleri (sadece DB boşsa)
        logger.LogInformation("🖼️ BannerSeeder başlatılıyor (sadece DB boşsa çalışır)...");
        Console.WriteLine("🖼️ BannerSeeder başlatılıyor (sadece DB boşsa çalışır)...");
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

// ═══════════════════════════════════════════════════════════════
// HTTPS & GÜVENLİK MIDDLEWARE'LERİ
// Production'da HTTPS zorunlu, Development'ta opsiyonel
// NEDEN: Ödeme ve kişisel veri trafiğinin şifrelenmesi yasal zorunluluk
// ═══════════════════════════════════════════════════════════════
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
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
    // GÜVENLİK: Timing-safe karşılaştırma ve sadece header-based token
    if (!string.IsNullOrWhiteSpace(prerenderBypassToken))
    {
        // Only consider bypass for dedicated prerender routes
        if (path.StartsWith("/api/prerender" ) || path.StartsWith("/api/prerender/list"))
        {
            var headerToken = context.Request.Headers["X-Prerender-Token"].ToString();

            // Timing-safe string karşılaştırma (timing attack'e karşı korumalı)
            if (!string.IsNullOrWhiteSpace(headerToken) &&
                headerToken.Length == prerenderBypassToken.Length)
            {
                var headerBytes = System.Text.Encoding.UTF8.GetBytes(headerToken);
                var expectedBytes = System.Text.Encoding.UTF8.GetBytes(prerenderBypassToken);

                if (System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(
                    headerBytes, expectedBytes))
                {
                    return false; // Bypass rate limiting
                }
            }
        }
    }

    return true;
}, branch =>
{
    branch.UseRateLimiter();
});
app.UseAuthentication();
app.UseAuthorization();

// ═══════════════════════════════════════════════════════════════════════════════
// HANGFIRE DASHBOARD - Zamanlanmış Görev Yönetimi
// /hangfire endpoint'inde admin paneli
// ═══════════════════════════════════════════════════════════════════════════════
app.UseHangfireDashboard("/hangfire", new Hangfire.DashboardOptions
{
    DashboardTitle = "Mikro ERP Sync Jobs",
    // Sadece Admin rolündeki kullanıcılar erişebilir
    Authorization = new[] { new HangfireAuthorizationFilter() },
    // Read-only modunda çalıştır (default - dashboard'dan kontrol edilir)
    IsReadOnlyFunc = _ => false // HangfireAuthorizationFilter role kontrolü yapıyor
});

// Hangfire Job'larını Kaydet (uygulama başlangıcında)
using (var scope = app.Services.CreateScope())
{
    try
    {
        var jobScheduler = scope.ServiceProvider.GetRequiredService<ECommerce.Core.Interfaces.Jobs.IMikroJobScheduler>();
        jobScheduler.RegisterAllJobs();
        app.Logger.LogInformation("✅ Hangfire Mikro sync job'ları başarıyla kaydedildi");
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(ex, "⚠️ Hangfire job kayıt sırasında hata oluştu - Job'lar manuel olarak kaydedilmeli");
    }
}

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

// =============================================
// SignalR Hub Endpoint'leri
// Gerçek zamanlı bildirim kanalları
// =============================================
// Müşteri sipariş takibi için hub
app.MapHub<OrderHub>("/hubs/order")
    .RequireCors("SignalR"); // SignalR CORS policy

// Admin bildirimleri için hub (yeni siparişler, teslimat sorunları)
app.MapHub<AdminNotificationHub>("/hubs/admin")
    .RequireCors("SignalR");

// Kurye bildirimleri için hub (atamalar, iptal bildirimleri)
app.MapHub<CourierHub>("/hubs/courier")
    .RequireCors("SignalR");

// Market Görevlisi (Store Attendant) bildirimleri için hub
// Sipariş onaylandığında, hazırlanmaya başlandığında bildirim alır
app.MapHub<StoreAttendantHub>("/hubs/store")
    .RequireCors("SignalR");

// Sevkiyat Görevlisi (Dispatcher) bildirimleri için hub
// Sipariş hazır olduğunda, kurye atandığında bildirim alır
app.MapHub<DispatcherHub>("/hubs/dispatch")
    .RequireCors("SignalR");

app.MapControllers();
app.Run();
