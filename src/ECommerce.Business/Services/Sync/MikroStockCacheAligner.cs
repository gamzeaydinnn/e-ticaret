using ECommerce.Data.Context;
using ECommerce.Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ECommerce.Business.Services.Sync
{
    /// <summary>
    /// Başarılı ToERP belge sonrası MikroProductCache stok hizalaması (Faz 4).
    /// Sipariş düşümü / iade artışı cache'e yansır; FromERP overwrite gecikmesini azaltır.
    /// </summary>
    public static class MikroStockCacheAligner
    {
        public static async Task ApplyDeltaAsync(
            ECommerceDbContext db,
            IEnumerable<(string Sku, decimal QuantityDelta)> lines,
            ILogger logger,
            CancellationToken cancellationToken = default)
        {
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line.Sku) || line.QuantityDelta == 0)
                {
                    continue;
                }

                var sku = line.Sku.Trim();
                var cache = await db.Set<MikroProductCache>()
                    .FirstOrDefaultAsync(c => c.StokKod == sku, cancellationToken);

                if (cache == null)
                {
                    logger.LogDebug(
                        "[MikroCacheAlign] Cache satırı yok, atlandı. SKU={Sku}, Delta={Delta}",
                        sku, line.QuantityDelta);
                    continue;
                }

                var before = cache.SatilabilirMiktar > 0 ? cache.SatilabilirMiktar : cache.DepoMiktari;
                var after = Math.Max(0, before + line.QuantityDelta);
                cache.DepoMiktari = after;
                cache.SatilabilirMiktar = after;
                cache.GuncellemeTarihi = DateTime.UtcNow;

                logger.LogInformation(
                    "[MikroCacheAlign] SKU={Sku}, {Before} → {After} (delta={Delta})",
                    sku, before, after, line.QuantityDelta);
            }

            await db.SaveChangesAsync(cancellationToken);
        }

        public static string? ResolveSku(OrderItem item, Product? product = null)
        {
            if (!string.IsNullOrWhiteSpace(item.VariantSku))
            {
                return item.VariantSku.Trim();
            }

            if (!string.IsNullOrWhiteSpace(product?.SKU))
            {
                return product.SKU.Trim();
            }

            return null;
        }
    }
}
