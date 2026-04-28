using ECommerce.Data.Context;
using ECommerce.Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ECommerce.API.Data
{
    /// <summary>
    /// Uygulama başlangıcında kritik kategori verilerinin varlığını garanti eder.
    /// 
    /// NEDEN: "Diğer" kategorisi ve wildcard mapping olmadan,
    /// Mikro'dan gelen eşlenememiş ürünler FK violation'a neden olur.
    /// Bu seeder her startup'ta idempotent çalışarak veri bütünlüğünü sağlar.
    /// </summary>
    public class CategorySeeder
    {
        // Sabit slug — kod genelinde bu slug ile "Diğer" kategorisi aranır
        public const string UncategorizedSlug = "diger";
        public const string UncategorizedName = "Diğer";

        public static async Task SeedAsync(ECommerceDbContext context)
        {
            try
            {
                Console.WriteLine("[CategorySeeder] 🔍 Kategoriler kontrol ediliyor...");

                // 1. "Diğer" kategorisini garanti et — eşlenemeyen ürünlerin güvenli limanı
                var digerCategory = await EnsureUncategorizedCategoryAsync(context);

                // 2. Wildcard (*) mapping'i garanti et — tüm eşlenemeyen ürünler "Diğer"e düşer
                await EnsureWildcardMappingAsync(context, digerCategory.Id);

                // 3. IsActive null/false olanları düzelt (legacy uyumluluk)
                await FixInactiveCategoriesAsync(context);

                // ADIM 13: Startup safety — geçersiz CategoryId'li ürünleri "Diğer"e taşı
                // NEDEN: Uygulama yeniden başladığında, arada silinmiş kategorilere 
                // referans veren ürünler FK violation'a yol açabilir.
                await FixOrphanProductsAsync(context, digerCategory.Id);

                // ADIM 13: ParentId bütünlüğü — silinmiş parent'a referans veren kategorileri düzelt
                await FixOrphanSubcategoriesAsync(context);

                Console.WriteLine("[CategorySeeder] ✅ Kategori seed + validasyon tamamlandı.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CategorySeeder] ❌ Hata: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// "Diğer" kategorisinin varlığını garanti eder.
        /// NEDEN: Mikro'dan gelen hiçbir mapping'e uymayan ürünler bu kategoriye atanır.
        /// Böylece CategoryId FK violation asla oluşmaz.
        /// </summary>
        private static async Task<Category> EnsureUncategorizedCategoryAsync(ECommerceDbContext context)
        {
            var existing = await context.Categories
                .FirstOrDefaultAsync(c => c.Slug == UncategorizedSlug);

            if (existing != null)
            {
                Console.WriteLine($"[CategorySeeder] ℹ️ '{UncategorizedName}' kategorisi zaten mevcut (Id={existing.Id})");
                return existing;
            }

            var diger = new Category
            {
                Name = UncategorizedName,
                Description = "Henüz kategorize edilmemiş ürünler. Mikro'dan gelen ve eşlenememiş ürünler burada toplanır.",
                Slug = UncategorizedSlug,
                SortOrder = 999,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            context.Categories.Add(diger);
            await context.SaveChangesAsync();

            Console.WriteLine($"[CategorySeeder] ✅ '{UncategorizedName}' kategorisi oluşturuldu (Id={diger.Id})");
            return diger;
        }

        /// <summary>
        /// Wildcard (*) MikroCategoryMapping kaydının varlığını garanti eder.
        /// NEDEN: CategoryMappingService.FindMappingAsync() son adımda "*" wildcard'ını arar.
        /// Bu kayıt yoksa eşlenememiş ürünler null mapping alır → hardcode CategoryId=1 fallback'i çalışır.
        /// Bu seed ile artık tüm eşlenememiş ürünler "Diğer"e yönlendirilir.
        /// </summary>
        private static async Task EnsureWildcardMappingAsync(ECommerceDbContext context, int digerCategoryId)
        {
            var existing = await context.Set<MikroCategoryMapping>()
                .FirstOrDefaultAsync(m => m.MikroAnagrupKod == "*");

            if (existing != null)
            {
                // CategoryId güncel mi kontrol et
                if (existing.CategoryId != digerCategoryId)
                {
                    existing.CategoryId = digerCategoryId;
                    existing.UpdatedAt = DateTime.UtcNow;
                    await context.SaveChangesAsync();
                    Console.WriteLine($"[CategorySeeder] 🔄 Wildcard mapping CategoryId güncellendi → {digerCategoryId}");
                }
                else
                {
                    Console.WriteLine("[CategorySeeder] ℹ️ Wildcard (*) mapping zaten mevcut");
                }
                return;
            }

            var wildcardMapping = new MikroCategoryMapping
            {
                MikroAnagrupKod = "*",
                MikroAltgrupKod = null,
                MikroMarkaKod = null,
                CategoryId = digerCategoryId,
                Priority = 999,
                IsActive = true,
                MikroGrupAciklama = "Varsayılan eşleme — hiçbir kurala uymayan ürünler 'Diğer' kategorisine atanır",
                Notes = "CategorySeeder tarafından otomatik oluşturuldu",
                CreatedAt = DateTime.UtcNow
            };

            context.Set<MikroCategoryMapping>().Add(wildcardMapping);
            await context.SaveChangesAsync();

            Console.WriteLine($"[CategorySeeder] ✅ Wildcard (*) mapping oluşturuldu → CategoryId={digerCategoryId}");
        }

        /// <summary>
        /// IsActive durumu hatalı kategorileri düzeltir (legacy uyumluluk).
        /// </summary>
        private static async Task FixInactiveCategoriesAsync(ECommerceDbContext context)
        {
            var categories = await context.Categories.ToListAsync();
            Console.WriteLine($"[CategorySeeder] 📊 Toplam {categories.Count} kategori bulundu");

            int updatedCount = 0;
            foreach (var category in categories)
            {
                if (!category.IsActive)
                {
                    category.IsActive = true;
                    updatedCount++;
                    Console.WriteLine($"  ✅ Kategori güncelleştirildi: {category.Name} -> IsActive = true");
                }
            }

            if (updatedCount > 0)
            {
                await context.SaveChangesAsync();
                Console.WriteLine($"[CategorySeeder] ✅ {updatedCount} kategori güncellendi");
            }
        }

        /// <summary>
        /// ADIM 13: Geçersiz CategoryId'li ürünleri "Diğer" kategorisine taşır.
        /// NEDEN: Silinmiş veya varolmayan kategorilere referans veren ürünler 
        /// runtime'da FK violation veya frontend'de boş kategori gösterimine neden olur.
        /// Bu kontrol her startup'ta çalışarak veri bütünlüğünü sağlar.
        /// </summary>
        private static async Task FixOrphanProductsAsync(ECommerceDbContext context, int digerCategoryId)
        {
            var validCategoryIds = await context.Categories
                .Select(c => c.Id)
                .ToListAsync();
            var validSet = new HashSet<int>(validCategoryIds);

            var orphanProducts = await context.Products
                .Where(p => !validSet.Contains(p.CategoryId))
                .ToListAsync();

            if (orphanProducts.Count == 0)
                return;

            foreach (var product in orphanProducts)
            {
                product.CategoryId = digerCategoryId;
                product.UpdatedAt = DateTime.UtcNow;
            }

            await context.SaveChangesAsync();
            Console.WriteLine($"[CategorySeeder] ⚠ ADIM 13: {orphanProducts.Count} yetim ürün 'Diğer'e taşındı");
        }

        /// <summary>
        /// ADIM 13: ParentId'si geçersiz olan alt kategorileri düzeltir.
        /// NEDEN: Parent kategori silinirse alt kategoriler orphan kalır.
        /// Bu kategorileri root seviyeye taşıyoruz (ParentId=null).
        /// </summary>
        private static async Task FixOrphanSubcategoriesAsync(ECommerceDbContext context)
        {
            var allCategoryIds = await context.Categories
                .Select(c => c.Id)
                .ToListAsync();
            var validSet = new HashSet<int>(allCategoryIds);

            var orphanSubcategories = await context.Categories
                .Where(c => c.ParentId.HasValue && !validSet.Contains(c.ParentId.Value))
                .ToListAsync();

            if (orphanSubcategories.Count == 0)
                return;

            foreach (var cat in orphanSubcategories)
            {
                cat.ParentId = null; // Root seviyeye taşı
                cat.UpdatedAt = DateTime.UtcNow;
            }

            await context.SaveChangesAsync();
            Console.WriteLine($"[CategorySeeder] ⚠ ADIM 13: {orphanSubcategories.Count} yetim alt kategori root'a taşındı");
        }
    }
}
