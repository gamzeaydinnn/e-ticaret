using System;
using System.Linq;
using System.Threading.Tasks;
using ECommerce.Data.Context;
using ECommerce.Entities.Concrete;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ECommerce.API.Data
{
    public static class BannerSeeder
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<ECommerceDbContext>();
            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

            try
            {
                logger.LogInformation("🔍 BannerSeeder: Başlatılıyor...");

                if (context.Banners.Any())
                {
                    logger.LogInformation("✅ BannerSeeder: Bannerlar zaten mevcut, seed atlandı");
                    return;
                }

                logger.LogInformation("📝 BannerSeeder: Örnek bannerlar oluşturuluyor...");

                var banners = new[]
                {
                    new Banner
                    {
                        Title = "Yeni Ürünler",
                        ImageUrl = "/images/banners/banner1.jpg",
                        LinkUrl = "/products?filter=new",
                        Type = "slider",
                        IsActive = true,
                        DisplayOrder = 1,
                        CreatedAt = DateTime.UtcNow
                    },
                    new Banner
                    {
                        Title = "İndirimli Ürünler",
                        ImageUrl = "/images/banners/banner2.jpg",
                        LinkUrl = "/products?filter=discount",
                        Type = "slider",
                        IsActive = true,
                        DisplayOrder = 2,
                        CreatedAt = DateTime.UtcNow
                    },
                    new Banner
                    {
                        Title = "Promosyon",
                        ImageUrl = "/images/banners/promo1.jpg",
                        LinkUrl = "/campaigns",
                        Type = "promo",
                        IsActive = true,
                        DisplayOrder = 3,
                        CreatedAt = DateTime.UtcNow
                    }
                };

                await context.Banners.AddRangeAsync(banners);
                await context.SaveChangesAsync();

                logger.LogInformation($"✅ BannerSeeder: {banners.Length} banner başarıyla oluşturuldu");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "❌ BannerSeeder: Hata oluştu");
                throw;
            }
        }
    }
}
