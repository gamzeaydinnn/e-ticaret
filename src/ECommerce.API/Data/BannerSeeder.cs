using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ECommerce.Data.Context;
using ECommerce.Entities.Concrete;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;

namespace ECommerce.API.Data
{
    /// <summary>
    /// Banner/Poster seed data oluşturucu
    /// Ana sayfa için varsayılan görselleri veritabanına ekler
    /// Admin panelinden "Varsayılana Sıfırla" butonuyla tekrar çağrılabilir
    /// 
    /// Seed işlemi sırasında görseller frontend/public/images/ klasöründen
    /// backend/uploads/banners/ klasörüne otomatik kopyalanır
    /// </summary>
    public static class BannerSeeder
    {
        /// <summary>
        /// Varsayılan banner'ları veritabanına ekler
        /// 3 adet slider (1200x400px) + 4 adet promo (300x200px) = 7 poster
        /// </summary>
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<ECommerceDbContext>();
            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
            var environment = serviceProvider.GetRequiredService<IHostEnvironment>();

            try
            {
                logger.LogInformation("🖼️ BannerSeeder: Başlatılıyor...");
                Console.WriteLine("🖼️ BannerSeeder: Başlatılıyor...");

                // Eğer zaten banner varsa, seed etme (ilk çalıştırma için)
                if (context.Banners.Any())
                {
                    Console.WriteLine("ℹ️ BannerSeeder: Veritabanında zaten banner mevcut, seed atlanıyor");
                    logger.LogInformation("ℹ️ BannerSeeder: Veritabanında zaten banner mevcut, seed atlanıyor");
                    return;
                }

                // Görsel dosyalarını kopyala
                CopyBannerImages(environment.ContentRootPath, logger);

                logger.LogInformation("📝 BannerSeeder: Varsayılan banner'lar oluşturuluyor...");
                Console.WriteLine("📝 BannerSeeder: Varsayılan banner'lar oluşturuluyor...");

                var banners = GetDefaultBanners();

                Console.WriteLine($"🔍 BannerSeeder: {banners.Length} banner ekleniyor...");
                await context.Banners.AddRangeAsync(banners);
                
                var saved = await context.SaveChangesAsync();
                Console.WriteLine($"✅ BannerSeeder: {saved} satır kaydedildi");
                
                logger.LogInformation("✅ BannerSeeder: {Count} banner başarıyla oluşturuldu", banners.Length);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "❌ BannerSeeder: Hata oluştu");
                Console.WriteLine($"❌ BannerSeeder: Hata - {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Tüm banner'ları silip varsayılanları yeniden ekler
        /// Admin panelinden "Varsayılana Sıfırla" için kullanılır
        /// </summary>
        public static async Task ResetToDefaultAsync(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<ECommerceDbContext>();
            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
            var environment = serviceProvider.GetRequiredService<IHostEnvironment>();

            try
            {
                logger.LogInformation("🔄 BannerSeeder: Varsayılana sıfırlanıyor...");
                Console.WriteLine("🔄 BannerSeeder: Varsayılana sıfırlanıyor...");

                // Tüm mevcut banner'ları sil
                var existingBanners = context.Banners.ToList();
                if (existingBanners.Any())
                {
                    context.Banners.RemoveRange(existingBanners);
                    await context.SaveChangesAsync();
                    Console.WriteLine($"🗑️ BannerSeeder: {existingBanners.Count} eski banner silindi");
                }

                // Görsel dosyalarını kopyala
                CopyBannerImages(environment.ContentRootPath, logger);

                // Varsayılanları ekle
                var banners = GetDefaultBanners();
                await context.Banners.AddRangeAsync(banners);
                var saved = await context.SaveChangesAsync();
                
                Console.WriteLine($"✅ BannerSeeder: {saved} varsayılan banner eklendi");
                logger.LogInformation("✅ BannerSeeder: Varsayılana sıfırlama tamamlandı");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "❌ BannerSeeder: Reset sırasında hata oluştu");
                throw;
            }
        }

        /// <summary>
        /// Banner görsellerini frontend/public/images/ klasöründen
        /// backend/uploads/banners/ klasörüne kopyalar
        /// 
        /// NEDEN: Seed data gerçek görselleri kullanabilmek için
        /// geliştirme ortamında frontend klasöründeki görselleri backend'e kopyalıyoruz
        /// 
        /// Production'da bu görseller deployment sırasında zaten uploads klasörüne konulur
        /// </summary>
        private static void CopyBannerImages(string contentRootPath, ILogger logger)
        {
            try
            {
                // Backend uploads/banners klasörü
                var uploadsPath = Path.Combine(contentRootPath, "uploads", "banners");
                if (!Directory.Exists(uploadsPath))
                {
                    Directory.CreateDirectory(uploadsPath);
                }

                // Frontend public/images klasörü (development ortamı için)
                // Proje yapısı: backend/src/ECommerce.API/ ve frontend/ aynı seviyede
                var apiDir = new DirectoryInfo(contentRootPath);
                var srcDir = apiDir.Parent; // src klasörü
                var projectRoot = srcDir?.Parent?.FullName; // proje root
                
                if (string.IsNullOrEmpty(projectRoot))
                {
                    logger.LogWarning("⚠️ Proje root klasörü bulunamadı, görsel kopyalama atlanıyor");
                    return;
                }

                var frontendImagesPath = Path.Combine(projectRoot, "frontend", "public", "images");
                if (!Directory.Exists(frontendImagesPath))
                {
                    logger.LogWarning("⚠️ Frontend images klasörü bulunamadı: {Path}", frontendImagesPath);
                    return;
                }

                // Kopyalanacak dosya listesi (seed data'da kullanılanlar)
                var imagesToCopy = new[]
                {
                    "taze-dogal-indirim-banner.png",
                    "ilk-alisveris-indirim-banner.png",
                    "golkoy-banner-1.png",
                    "meyve-reyonu-banner.png",
                    "taze-gunluk-lezzetli.png",
                    "ozel-fiyat-koy-sutu.png",
                    "pinar-yogurt-banner.jpg"
                };

                int copiedCount = 0;
                foreach (var imageName in imagesToCopy)
                {
                    var sourcePath = Path.Combine(frontendImagesPath, imageName);
                    var destPath = Path.Combine(uploadsPath, imageName);

                    // Hedef dosya yoksa veya kaynak daha yeniyse kopyala
                    if (File.Exists(sourcePath))
                    {
                        if (!File.Exists(destPath) || 
                            File.GetLastWriteTimeUtc(sourcePath) > File.GetLastWriteTimeUtc(destPath))
                        {
                            File.Copy(sourcePath, destPath, overwrite: true);
                            copiedCount++;
                        }
                    }
                    else
                    {
                        logger.LogWarning("⚠️ Görsel bulunamadı: {ImageName}", imageName);
                    }
                }

                if (copiedCount > 0)
                {
                    Console.WriteLine($"📁 BannerSeeder: {copiedCount} görsel dosyası uploads/banners/ klasörüne kopyalandı");
                    logger.LogInformation("📁 {Count} banner görseli kopyalandı", copiedCount);
                }
            }
            catch (Exception ex)
            {
                // Görsel kopyalama hatası kritik değil, seed devam edebilir
                logger.LogWarning(ex, "⚠️ Banner görselleri kopyalanırken hata oluştu (kritik değil)");
                Console.WriteLine($"⚠️ Görsel kopyalama hatası (kritik değil): {ex.Message}");
            }
        }

        /// <summary>
        /// Varsayılan banner listesini döndürür
        /// 3 Slider + 4 Promo = 7 Banner
        /// 
        /// NOT: Seed işlemi sırasında görseller frontend/public/images/ klasöründen
        /// backend/uploads/banners/ klasörüne kopyalanır
        /// </summary>
        private static Banner[] GetDefaultBanners()
        {
            var now = DateTime.UtcNow;
            
            return new[]
            {
                // ==================== SLIDER BANNER'LAR (3 adet) ====================
                // Önerilen boyut: 1200x400px
                // Gerçek görsel dosyaları kullanılıyor
                
                new Banner
                {
                    Title = "Taze ve Doğal Ürünler",
                    SubTitle = "Gölköy'ün en taze meyve ve sebzeleri kapınızda",
                    Description = "Ana sayfa slider - taze ürünler kampanyası",
                    ImageUrl = "/uploads/banners/taze-dogal-indirim-banner.png",
                    LinkUrl = "/products?category=meyve-ve-sebze",
                    ButtonText = "Hemen İncele",
                    Type = "slider",
                    Position = "homepage-top",
                    IsActive = true,
                    DisplayOrder = 1,
                    CreatedAt = now
                },
                new Banner
                {
                    Title = "İlk Alışverişe Özel %20 İndirim",
                    SubTitle = "HOSGELDIN kodu ile tüm siparişlerinizde geçerli",
                    Description = "Ana sayfa slider - ilk alışveriş kampanyası",
                    ImageUrl = "/uploads/banners/ilk-alisveris-indirim-banner.png",
                    LinkUrl = "/products",
                    ButtonText = "Alışverişe Başla",
                    Type = "slider",
                    Position = "homepage-top",
                    IsActive = true,
                    DisplayOrder = 2,
                    CreatedAt = now
                },
                new Banner
                {
                    Title = "Gölköy Gurme Market",
                    SubTitle = "Doğal ve organik ürünlerde güvenin adresi",
                    Description = "Ana sayfa slider - marka tanıtımı",
                    ImageUrl = "/uploads/banners/golkoy-banner-1.png",
                    LinkUrl = "/products",
                    ButtonText = "Kampanyayı Gör",
                    Type = "slider",
                    Position = "homepage-top",
                    IsActive = true,
                    DisplayOrder = 3,
                    CreatedAt = now
                },
                
                // ==================== PROMO KARTLAR (4 adet) ====================
                // Önerilen boyut: 300x200px
                // Gerçek görsel dosyaları kullanılıyor
                
                new Banner
                {
                    Title = "Meyve Reyonu",
                    SubTitle = "Taze meyveler",
                    Description = "Promo kart - meyve kategorisi",
                    ImageUrl = "/uploads/banners/meyve-reyonu-banner.png",
                    LinkUrl = "/products?category=meyve",
                    ButtonText = "Keşfet",
                    Type = "promo",
                    Position = "homepage-middle",
                    IsActive = true,
                    DisplayOrder = 4,
                    CreatedAt = now
                },
                new Banner
                {
                    Title = "Taze Günlük Ürünler",
                    SubTitle = "Her gün taze lezzetler",
                    Description = "Promo kart - günlük ürünler",
                    ImageUrl = "/uploads/banners/taze-gunluk-lezzetli.png",
                    LinkUrl = "/products?category=gunluk",
                    ButtonText = "Keşfet",
                    Type = "promo",
                    Position = "homepage-middle",
                    IsActive = true,
                    DisplayOrder = 5,
                    CreatedAt = now
                },
                new Banner
                {
                    Title = "Süt Ürünleri",
                    SubTitle = "Köy sütü ve organik peynirleri",
                    Description = "Promo kart - süt ürünleri kategorisi",
                    ImageUrl = "/uploads/banners/ozel-fiyat-koy-sutu.png",
                    LinkUrl = "/products?category=sut-urunleri",
                    ButtonText = "Keşfet",
                    Type = "promo",
                    Position = "homepage-middle",
                    IsActive = true,
                    DisplayOrder = 6,
                    CreatedAt = now
                },
                new Banner
                {
                    Title = "Pınar Yoğurt",
                    SubTitle = "Doğal ve sağlıklı",
                    Description = "Promo kart - yoğurt kampanyası",
                    ImageUrl = "/uploads/banners/pinar-yogurt-banner.jpg",
                    LinkUrl = "/products?category=sut-urunleri",
                    ButtonText = "Keşfet",
                    Type = "promo",
                    Position = "homepage-middle",
                    IsActive = true,
                    DisplayOrder = 7,
                    CreatedAt = now
                }
            };
        }
    }
}
