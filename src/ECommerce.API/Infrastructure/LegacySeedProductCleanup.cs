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
    /// Eski demo/seed ürünlerini siler veya sipariş geçmişi varsa pasifleştirir.
    /// Production'da idempotent; her deploy'da güvenle çalıştırılabilir.
    /// </summary>
    public static class LegacySeedProductCleanup
    {
        public static async Task RunAsync(IServiceProvider services)
        {
            var db = services.GetRequiredService<ECommerceDbContext>();
            var logger = services.GetRequiredService<ILoggerFactory>()
                .CreateLogger("LegacySeedProductCleanup");

            var legacySkus = LegacySeedProductSkus.All.ToList();
            var legacySlugs = LegacySeedProductSkus.LegacySlugs.ToList();
            var legacyNames = LegacySeedProductSkus.LegacyNames.ToList();

            var legacyProducts = await db.Products
                .Where(product =>
                    (product.SKU != null && legacySkus.Contains(product.SKU))
                    || legacySlugs.Contains(product.Slug)
                    || legacyNames.Contains(product.Name))
                .Select(product => new { product.Id, product.SKU, product.Name, product.IsActive })
                .ToListAsync();

            if (legacyProducts.Count == 0)
            {
                logger.LogInformation("Legacy seed ürün bulunamadı, cleanup atlandı.");
                return;
            }

            var legacyIds = legacyProducts.Select(product => product.Id).ToList();

            var orderedProductIds = await db.OrderItems
                .Where(item => legacyIds.Contains(item.ProductId))
                .Select(item => item.ProductId)
                .Distinct()
                .ToListAsync();

            var deletableIds = legacyIds.Except(orderedProductIds).ToList();

            if (deletableIds.Count > 0)
            {
                await db.HomeBlockProducts
                    .Where(link => deletableIds.Contains(link.ProductId))
                    .ExecuteDeleteAsync();
                await db.CartItems
                    .Where(item => deletableIds.Contains(item.ProductId))
                    .ExecuteDeleteAsync();
                await db.Favorites
                    .Where(fav => deletableIds.Contains(fav.ProductId))
                    .ExecuteDeleteAsync();
                await db.ProductReviews
                    .Where(review => deletableIds.Contains(review.ProductId))
                    .ExecuteDeleteAsync();
                await db.ProductImages
                    .Where(image => deletableIds.Contains(image.ProductId))
                    .ExecuteDeleteAsync();
                await db.CouponProducts
                    .Where(cp => deletableIds.Contains(cp.ProductId))
                    .ExecuteDeleteAsync();

                var deleted = await db.Products
                    .Where(product => deletableIds.Contains(product.Id))
                    .ExecuteDeleteAsync();

                logger.LogWarning(
                    "Legacy seed cleanup: {Deleted} demo ürün silindi. SKU/ad: {Skus}",
                    deleted,
                    string.Join(", ", legacyProducts
                        .Where(product => deletableIds.Contains(product.Id))
                        .Select(product => product.SKU ?? product.Name)));
            }

            if (orderedProductIds.Count > 0)
            {
                var deactivated = await db.Products
                    .Where(product => orderedProductIds.Contains(product.Id) && product.IsActive)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(product => product.IsActive, false));

                logger.LogWarning(
                    "Legacy seed cleanup: {Count} demo ürün sipariş geçmişi nedeniyle pasifleştirildi (silinmedi).",
                    deactivated);
            }
        }
    }
}
