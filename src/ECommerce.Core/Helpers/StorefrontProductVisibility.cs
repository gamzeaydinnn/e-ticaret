using System.Collections.Generic;
using ECommerce.Entities.Concrete;

namespace ECommerce.Core.Helpers
{
    public static class StorefrontProductVisibility
    {
        /// <summary>
        /// Vitrin/ana sayfa için ürün görünürlüğü.
        /// Mikro ürünleri + admin panelinden eklenen manuel yerel ürünler görünür; demo seed'ler hariç.
        /// </summary>
        public static bool IsVisible(Product? product, IReadOnlySet<string>? mikroVisibleSkus)
        {
            if (product == null || !product.IsActive)
                return false;

            if (LegacySeedProductSkus.IsLegacyProduct(product.SKU, product.Slug, product.Name))
                return false;

            if (IsManualLocalProduct(product.SKU, mikroVisibleSkus))
                return true;

            if (mikroVisibleSkus != null && mikroVisibleSkus.Count > 0)
            {
                var sku = product.SKU?.Trim();
                return !string.IsNullOrEmpty(sku) && mikroVisibleSkus.Contains(sku);
            }

            return true;
        }

        /// <summary>
        /// Mikro kataloğunda olmayan, admin tarafından eklenen yerel ürünler.
        /// </summary>
        public static bool IsManualLocalProduct(string? sku, IReadOnlySet<string>? mikroVisibleSkus)
        {
            var normalizedSku = sku?.Trim();

            if (string.IsNullOrEmpty(normalizedSku))
                return true;

            if (normalizedSku.StartsWith("PRD", System.StringComparison.OrdinalIgnoreCase))
                return true;

            if (mikroVisibleSkus != null && mikroVisibleSkus.Count > 0)
                return !mikroVisibleSkus.Contains(normalizedSku);

            return false;
        }
    }
}
