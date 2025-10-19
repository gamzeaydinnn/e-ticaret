using ECommerce.Data.Context;
using Microsoft.EntityFrameworkCore;
using ECommerce.Business.Services.Managers;
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

// DbContext ekle - SQL Server kullan
builder.Services.AddDbContext<ECommerceDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services
    .AddIdentityCore<User>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequireDigit = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.Password.RequiredLength = 6;
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
builder.Services.AddScoped<MicroSyncManager>();
// repositories
builder.Services.AddScoped<IBrandRepository, BrandRepository>();
builder.Services.AddScoped<IDiscountRepository, DiscountRepository>();
builder.Services.AddScoped<IReviewRepository, ReviewRepository>();
builder.Services.AddScoped<IAddressRepository, AddressRepository>();
builder.Services.AddScoped<ICouponRepository, CouponRepository>();


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

// Rate Limiting (Global, IP başına)
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        var key = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 100,
            Window = TimeSpan.FromMinutes(1),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 0
        });
    });
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

// Controller ekle
builder.Services.AddControllers();

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
        // Migrations yoksa dev ortamında EnsureCreated; varsa migrate
        try
        {
            var pending = db.Database.GetPendingMigrations();
            if (pending != null && pending.Any())
                db.Database.Migrate();
            else
                db.Database.EnsureCreated();
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

app.UseHttpsRedirection();
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseCors("Default");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();
