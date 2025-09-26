using ECommerce.Data.Context;
using Microsoft.EntityFrameworkCore;
using ECommerce.Infrastructure.Services.Email;
using ECommerce.Infrastructure.Config;
using ECommerce.Business.Services.Managers;
using ECommerce.Business.Services.Interfaces;   
using Microsoft.Extensions.Options;
using ECommerce.Core.Interfaces;
using ECommerce.Data.Repositories;
using ECommerce.Infrastructure.Services.Payment;
using ECommerce.Infrastructure.Services.BackgroundJobs;
using ECommerce.Infrastructure.Services.Micro;


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


// AppSettings’i konfigürasyondan al
builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("AppSettings"));


// EmailSender servisini AppSettings üzerinden kullanacak şekilde ekle
builder.Services.AddScoped<EmailSender>();
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
builder.Services.AddScoped<LocalSalesRepository>();
builder.Services.AddHostedService<StockSyncJob>();
builder.Services.AddScoped<IMicroService, MicroService>();

// Controller ekle
builder.Services.AddControllers();

// Swagger (isteğe bağlı)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthorization();

app.Run();
//Ne işe yarıyor?
//Bu kod, bir ASP.NET Core Web API uygulamasının başlangıç (startup) kodudur.
//Uygulama başlatılırken gerekli servisleri (DbContext, EmailSender, AppSettings) dependency injection konteynerine ekler,
//middleware yapılandırmasını yapar ve HTTP isteklerini karşılamak için controller'ları haritalar.
//Swagger, API dokümantasyonu için geliştirme ortamında etkinleştirilir.
//AppSettings, uygulama genelinde kullanılacak yapılandırma ayarlarını tutar ve
//EmailSender servisi bu ayarları kullanarak e-posta gönderme işlevselliği sağlar.
//DbContext, SQL Server veritabanı ile etkileşim için yapılandırılır.