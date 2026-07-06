using System;
using System.Linq;
using System.Threading.Tasks;
using ECommerce.Core.Helpers;
using ECommerce.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ECommerce.API.Infrastructure
{
    /// <summary>
    /// Eski demo/seed ürünlerini pasifleştirir ve ana sayfa bloklarından çıkarır.
    /// Production'da bir kez çalıştırılsa yeterli; idempotent.
    /// </summary>
    public static class LegacySeedProductCleanup
    {
        public static async Task RunAsync(IServiceProvider services)
        {
            var db = services.GetRequiredService<ECommerceDbContext>();
            var logger = services.GetRequiredService<ILoggerFactory>()
                .CreateLogger("LegacySeedProductCleanup");

            var legacySkus = LegacySeedProductSkus.All.ToList();

            var legacyProducts = await db.Products
                .Where(p => p.SKU != null && legacySkus.Contains(p.SKU))
                .Select(p => new { p.Id, p.SKU, p.IsActive })
                .ToListAsync();

            if (legacyProducts.Count == 0)
            {
                logger.LogInformation("Legacy seed ürün bulunamadı, cleanup atlandı.");
                return;
            }

            var legacyIds = legacyProducts.Select(p => p.Id).ToList();

            var removedLinks = await db.HomeBlockProducts
                .Where(h => legacyIds.Contains(h.ProductId))
                .ExecuteDeleteAsync();

            var deactivated = await db.Products
                .Where(p => legacyIds.Contains(p.Id) && p.IsActive)
                .ExecuteUpdateAsync(setters => setters.SetProperty(p => p.IsActive, false));

            logger.LogWarning(
                "Legacy seed cleanup: {ProductCount} ürün pasifleştirildi ({Deactivated} yeni), {LinkCount} home block bağlantısı silindi. SKU'lar: {Skus}",
                legacyProducts.Count,
                deactivated,
                removedLinks,
                string.Join(", ", legacyProducts.Select(p => p.SKU)));
        }
    }
}
