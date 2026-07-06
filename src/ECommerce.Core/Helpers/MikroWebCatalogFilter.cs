using System.Collections.Generic;
using System.Linq;
using ECommerce.Core.DTOs.Micro;
using ECommerce.Entities.Concrete;

namespace ECommerce.Core.Helpers
{
    /// <summary>
    /// Mikro ERP'den web kataloğuna yalnızca "webe gönderilecek" (sto_webe_gonderilecek_fl=1)
    /// ürünlerin alınmasını sağlar. Tüm Mikro stok kartları değil, web aktif subset kullanılır.
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
