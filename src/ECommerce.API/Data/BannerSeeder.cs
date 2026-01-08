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
                Console.WriteLine("🔍 BannerSeeder: Başlatılıyor...");

                // Banners tablosunu temizle (yeniden seed etmek için)
                Console.WriteLine("🔍 BannerSeeder: Mevcut bannerlar temizleniyor...");
                var existingBanners = context.Banners.ToList();
                if (existingBanners.Any())
                {
                    context.Banners.RemoveRange(existingBanners);
                    await context.SaveChangesAsync();
                    Console.WriteLine($"✅ BannerSeeder: {existingBanners.Count} eski banner silindi");
                }

                logger.LogInformation("📝 BannerSeeder: Örnek bannerlar oluşturuluyor...");
                Console.WriteLine("📝 BannerSeeder: Örnek bannerlar oluşturuluyor...");

                var banners = new[]
                {
                    new Banner
                    {
                        Title = "Taze ve Doğal İndirim",
                        ImageUrl = "/images/taze-dogal-indirim-banner.png",
                        LinkUrl = "/products?category=meyve-ve-sebze",
                        Type = "slider",
                        IsActive = true,
                        DisplayOrder = 1,
                        CreatedAt = DateTime.UtcNow
                    },
                    new Banner
                    {
                        Title = "Meyve Reyonu",
                        ImageUrl = "/images/meyve-reyonu-banner.png",
                        LinkUrl = "/products?category=meyve-ve-sebze",
                        Type = "slider",
                        IsActive = true,
                        DisplayOrder = 2,
                        CreatedAt = DateTime.UtcNow
                    },
                    new Banner
                    {
                        Title = "Gölköy Market",
                        ImageUrl = "/images/golkoy-banner-1.png",
                        LinkUrl = "/campaigns",
                        Type = "promo",
                        IsActive = true,
                        DisplayOrder = 3,
                        CreatedAt = DateTime.UtcNow
                    },
                    new Banner
                    {
                        Title = "İlk Alışveriş İndirimi",
                        ImageUrl = "/images/ilk-alisveris-indirim-banner.png",
                        LinkUrl = "/products",
                        Type = "slider",
                        IsActive = true,
                        DisplayOrder = 4,
                        CreatedAt = DateTime.UtcNow
                    }
                };

                Console.WriteLine("🔍 BannerSeeder: Bannerlar AddRangeAsync ile ekleniyor...");
                await context.Banners.AddRangeAsync(banners);
                Console.WriteLine($"🔍 BannerSeeder: {banners.Length} banner eklendi, SaveChangesAsync çağrılıyor...");
                
                var saved = await context.SaveChangesAsync();
                Console.WriteLine($"✅ BannerSeeder: SaveChangesAsync döndü, {saved} satır etkilendi");
                
                logger.LogInformation($"✅ BannerSeeder: {banners.Length} banner başarıyla oluşturuldu");
                Console.WriteLine($"✅ BannerSeeder: Log yazıldı");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "❌ BannerSeeder: Hata oluştu");
                throw;
            }
        }
    }
}
