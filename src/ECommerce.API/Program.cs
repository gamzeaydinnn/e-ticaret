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
// using ECommerce.Infrastructure.Services.BackgroundJobs;
// using Hangfire;
using ECommerce.Infrastructure.Services.MicroServices;
using Microsoft.AspNetCore.Authorization;
using ECommerce.Infrastructure.Services.Payment;
using ECommerce.Infrastructure.Config;


// using ECommerce.Infrastructure.Services.BackgroundJobs;
// using Hangfire;
// using Hangfire.SqlServer;

var builder = WebApplication.CreateBuilder(args);

// CORS (Frontend için izin ver)
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("http://localhost:3000", "http://localhost:3001")
              .AllowAnyHeader()
              .AllowAnyMethod());
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
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
        };
    });

// Repositories
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ICartRepository, CartRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();

// Services  
builder.Services.AddScoped<IUserService, UserManager>();
builder.Services.AddScoped<IProductService, ProductManager>();
builder.Services.AddScoped<IOrderService, OrderManager>();
builder.Services.AddScoped<ICartService, CartManager>();
builder.Services.AddScoped<IPaymentService, PaymentManager>();
builder.Services.AddScoped<IShippingService, ShippingManager>();
builder.Services.AddScoped<ProductManager>();
builder.Services.AddScoped<OrderManager>();
builder.Services.AddScoped<UserManager>();
builder.Services.AddScoped<CartManager>();
builder.Services.AddScoped<InventoryManager>();
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

// vs.

// builder.Services.AddScoped<StockSyncJob>();
// MicroService ve MicroSyncManager
builder.Services.AddScoped<IMicroService, ECommerce.Business.Services.Managers.MicroService>();
builder.Services.AddScoped<MicroSyncManager>();
builder.Services.AddScoped<IAuthService, AuthManager>();

// // builder.Services.AddScoped<StockSyncJob>();

builder.Services.AddAuthorization();

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

// Middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();
