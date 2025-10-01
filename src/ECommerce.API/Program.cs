using ECommerce.Data.Context;
using Microsoft.EntityFrameworkCore;
using ECommerce.Business.Services.Managers;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Core.Interfaces;
using ECommerce.Data.Repositories;

var builder = WebApplication.CreateBuilder(args);

// DbContext ekle
builder.Services.AddDbContext<ECommerceDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// AppSettings’i konfigürasyondan al
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

// Controller ekle
builder.Services.AddControllers();

// CORS (Frontend için izin ver)
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("http://localhost:3000", "http://localhost:3001")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

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

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthorization();

app.MapControllers();

app.Run();
//Ne işe yarıyor?
//Bu kod, bir ASP.NET Core Web API uygulamasının başlangıç (startup) kodudur.
//Uygulama başlatılırken gerekli servisleri (DbContext, EmailSender, AppSettings) dependency injection konteynerine ekler,
//middleware yapılandırmasını yapar ve HTTP isteklerini karşılamak için controller'ları haritalar.
//Swagger, API dokümantasyonu için geliştirme ortamında etkinleştirilir.
//AppSettings, uygulama genelinde kullanılacak yapılandırma ayarlarını tutar ve
//EmailSender servisi bu ayarları kullanarak e-posta gönderme işlevselliği sağlar.
//DbContext, SQL Server veritabanı ile etkileşim için yapılandırılır.