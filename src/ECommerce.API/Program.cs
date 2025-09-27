using ECommerce.Data.Context;
using Microsoft.EntityFrameworkCore;
using ECommerce.Business.Services.Managers;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Core.Interfaces;
using ECommerce.Data.Repositories;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using ECommerce.Infrastructure.Services.BackgroundJobs;
using Hangfire;
using Hangfire.SQLite;



var builder = WebApplication.CreateBuilder(args);
// CORS (Frontend için izin ver)
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("http://localhost:3000", "http://localhost:3001")
              .AllowAnyHeader()
              .AllowAnyMethod());
});
// DbContext ekle
builder.Services.AddDbContext<ECommerceDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));


// Hangfire
builder.Services.AddHangfire(config => 
    config.UseSQLiteStorage(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddHangfireServer();

var app = builder.Build();
app.UseHangfireDashboard();

// Recurring job
RecurringJob.AddOrUpdate<StockSyncJob>(
    job => job.RunOnce(), // StockSyncJob'da public async Task RunOnce() olmalı
    Cron.Hourly);



// JWT Auth 
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
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

// AppSettings’i konfigürasyondan al (Bu yorum main'den geldi, ama AppSettings konfigürasyonu HEAD'deydi. Sadece yorumu bırakıyorum.)
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

builder.Services.AddScoped<StockSyncJob>();

builder.Services.AddAuthorization();

app.UseAuthentication();
app.UseAuthorization();

// Controller ekle
builder.Services.AddControllers();

// Swagger (isteğe bağlı)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthorization();

app.MapControllers();
app.Run();