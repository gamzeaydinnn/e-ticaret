using System.Collections.Generic;
using ECommerce.Entities.Concrete;

namespace ECommerce.Core.Helpers
{
    public static class StorefrontProductVisibility
    {
        /// <summary>
        /// Vitrin/ana sayfa için ürün görünürlüğü.
        /// Mikro cache doluysa yalnızca cache'teki aktif SKU'lar; aksi halde legacy seed'ler hariç.
        /// </summary>
        public static bool IsVisible(Product? product, IReadOnlySet<string>? mikroVisibleSkus)
        {
            if (product == null || !product.IsActive)
                return false;

            if (LegacySeedProductSkus.IsLegacy(product.SKU))
                return false;

            if (mikroVisibleSkus != null && mikroVisibleSkus.Count > 0)
            {
                var sku = product.SKU?.Trim();
                return !string.IsNullOrEmpty(sku) && mikroVisibleSkus.Contains(sku);
            }

            return true;
        }
    }
}
