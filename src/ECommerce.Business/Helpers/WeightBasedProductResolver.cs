using ECommerce.Entities.Concrete;
using ECommerce.Entities.Enums;

namespace ECommerce.Business.Helpers
{
    /// <summary>
    /// Bir ürünün "değişken ağırlıklı (kg) mı yoksa adet/paket bazlı mı" olduğuna ve
    /// hangi birim fiyatın kullanılacağına karar veren TEK otorite (Single Source of Truth).
    /// </summary>
    public static class WeightBasedProductResolver
    {
        /// <summary>
        /// Ürünün değişken ağırlıklı (kg) olup olmadığını tek kuralla belirler.
        /// Null ürün güvenli biçimde <c>false</c> döndürür.
        /// </summary>
        public static bool ResolveIsWeightBased(Product? product)
        {
            if (product is null)
            {
                return false;
            }

            // 1) Paketli ürün / Konserve / Donuk & Dondurulmuş Gıda negatif sinyali HER ŞEYİ EZER.
            //    NEDEN: Eski senkronlarda "Konserve Meyve & Sebze" (2203), "Donuk Meyve & Sebze" (2204)
            //    veya isminde "ADET" / "PAKET" / "500 GR" geçen ürünlere yanlışlıkla IsWeightBased=true
            //    yazılmış olabilir. Bu ürünler kesinlikle paket/adet ile satılır, kg satışı yoktur.
            if (WeightBasedProductRules.HasPackagedNameSignal(product.Name) ||
                WeightBasedProductRules.IsPackagedOrExcludedCategory(
                    product.Category?.Name,
                    product.Category?.Slug,
                    product.Category?.MikroAltGrupKod,
                    product.Category?.MikroAnaGrupKod))
            {
                return false;
            }

            // 2) Negatif sinyal yoksa ve açıkça IsWeightBased=true işaretlenmişse kg.
            if (product.IsWeightBased)
            {
                return true;
            }

            // 3) Kütle birimi (kg/gram) tanımlıysa değişken ağırlıklıdır.
            if (IsMassWeightUnit(product.WeightUnit))
            {
                return true;
            }

            // 4) Birim fiyat (TL/kg) tanımlı ve birim 'adet' değilse değişken ağırlıklıdır.
            if (product.PricePerUnit > 0m && product.WeightUnit != WeightUnit.Piece)
            {
                return true;
            }

            // 5) Yapısal veri yetersiz: mevcut isim+kategori heuristiğine düş.
            return WeightBasedProductRules.IsVariableWeightKgProduct(
                product.Name,
                product.WeightUnit,
                product.Category?.Name);
        }

        /// <summary>
        /// Ürünün geçerli birim fiyatını döndürür.
        /// </summary>
        public static decimal ResolveUnitPrice(Product? product, bool isWeightBased)
        {
            if (product is null)
            {
                return 0m;
            }

            // 🔴 KRİTİK DÜZELTME: Veritabanındaki eski PricePerUnit kayıtları hatalı olduğu için,
            // tartılı ürünlerde de her zaman güncel olan Price veya SpecialPrice alanını baz alıyoruz.
            return product.SpecialPrice ?? product.Price;
        }

        /// <summary>
        /// Tespit ve birim fiyatı tek çağrıda döndüren kolaylık metodu.
        /// </summary>
        public static (bool IsWeightBased, decimal UnitPrice) Resolve(Product? product)
        {
            var isWeightBased = ResolveIsWeightBased(product);
            return (isWeightBased, ResolveUnitPrice(product, isWeightBased));
        }

        /// <summary>
        /// Birim, kütle bazlı (değişken ağırlıklı satışa uygun) mı?
        /// </summary>
        private static bool IsMassWeightUnit(WeightUnit unit) =>
            unit is WeightUnit.Kilogram or WeightUnit.Gram;
    }
}
