using System.Collections.Generic;
using System.Linq;
using ECommerce.Core.DTOs.Micro;
using ECommerce.Entities.Concrete;

namespace ECommerce.Core.Helpers
{
    /// <summary>
    /// Mikro web kataloğu: sto_webe_gonderilecek_fl=1.
    /// Satılabilir web kataloğu (admin/dashboard sayısı): ayrıca fiyat &gt; 0.
    /// </summary>
    public static class MikroWebCatalogFilter
    {
        public static List<MikroUnifiedProductDto> OnlyWebActive(IEnumerable<MikroUnifiedProductDto> products)
        {
            return products
                .Where(product => product.WebeGonderilecekFl)
                .ToList();
        }

        /// <summary>
        /// Admin/Dashboard ile vitrin stok dışı sayım için: web bayrağı + fiyatlı ürün.
        /// </summary>
        public static bool IsPricedWebCatalogProduct(decimal resolvedPrice, bool isWebActive)
        {
            return isWebActive && resolvedPrice > 0m;
        }

        /// <summary>
        /// Web kataloğunda gösterilebilir mi? Mikro web bayrağı zorunlu; yerel pasif kayıt hariç tutulur.
        /// </summary>
        public static bool IsWebCatalogProduct(MikroUnifiedProductDto mikroProduct, Product? localProduct = null)
        {
            if (mikroProduct == null || !mikroProduct.WebeGonderilecekFl)
                return false;

            if (localProduct != null && !localProduct.IsActive)
                return false;

            return true;
        }

        public static bool ResolveIsActive(MikroUnifiedProductDto mikroProduct, Product? localProduct)
        {
            if (!mikroProduct.WebeGonderilecekFl)
                return false;

            return localProduct?.IsActive ?? true;
        }
    }

}
