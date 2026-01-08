using ECommerce.Data.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace ECommerce.API.Data
{
    public class CategorySeeder
    {
        public static async Task SeedAsync(ECommerceDbContext context)
        {
            try
            {
                Console.WriteLine("[CategorySeeder] 🔍 Kategorilerin IsActive alanı kontrol ediliyor...");

                // Tüm NULL IsActive değerlerini true olarak ayarla
                var categories = await context.Categories.ToListAsync();
                Console.WriteLine($"[CategorySeeder] 📊 Toplam {categories.Count} kategori bulundu");
                
                int updatedCount = 0;

                foreach (var category in categories)
                {
                    if (category.IsActive == null || !category.IsActive)
                    {
                        category.IsActive = true;
                        updatedCount++;
                        Console.WriteLine($"  ✅ Kategori güncelleştirildi: {category.Name} -> IsActive = true");
                    }
                }

                if (updatedCount > 0)
                {
                    await context.SaveChangesAsync();
                    Console.WriteLine($"[CategorySeeder] ✅ {updatedCount} kategori güncellendi ve kaydedildi!");
                }
                else
                {
                    Console.WriteLine("[CategorySeeder] ℹ️ Tüm kategoriler zaten aktif durumdadır.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CategorySeeder] ❌ Hata: {ex.Message}");
                throw;
            }
        }
    }
}
