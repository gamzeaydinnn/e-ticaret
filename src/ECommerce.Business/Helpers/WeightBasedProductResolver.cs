using ECommerce.Entities.Concrete;
using ECommerce.Entities.Enums;

namespace ECommerce.Business.Helpers
{
    /// <summary>
    /// Bir ürünün "değişken ağırlıklı (kg) mı yoksa adet bazlı mı" olduğuna ve
    /// hangi birim fiyatın kullanılacağına karar veren TEK otorite (Single Source of Truth).
    ///
    /// NEDEN BU SINIF VAR?
    /// Daha önce bu karar dört ayrı yerde (CartManager, PricingEngine, OrderManager ve frontend)
    /// birbirinden FARKLI kurallarla veriliyordu. Örneğin eski <see cref="WeightBasedProductRules.IsVariableWeightKgProduct"/>
    /// kuralı, ürün adında "KG" token'ı yoksa <c>IsWeightBased=true</c> ve <c>WeightUnit=Kilogram</c>
    /// olsa bile ürünü adet sayıyordu. Bu tutarsızlık, sepette gösterilen tutar ile 3D Secure'a giden
    /// tutarın farklı olmasının kök nedeniydi.
    ///
    /// Bu çözümleyici, kararı yapısal/açık verilere (IsWeightBased, WeightUnit, PricePerUnit) öncelik
    /// vererek tek noktadan üretir; isim+kategori heuristiği yalnızca yapısal veri yetersiz kaldığında
    /// SON ÇARE olarak kullanılır. Tüm katmanlar bu sınıfı çağırarak aynı sonucu üretmelidir.
    ///
    /// ÖNCELİK SIRASI (yüksekten düşüğe):
    /// 1. Açık bayrak: <c>IsWeightBased == true</c> → kg (admin/senkron açıkça işaretlemiş; en yüksek güven).
    /// 2. Negatif isim sinyali: "500 GR" / "1 LT" / "ADET" → adet (paketli ürün).
    /// 3. Yapısal birim: <c>WeightUnit</c> kütle birimi (Kilogram/Gram) → kg.
    /// 4. Yapısal fiyat: <c>PricePerUnit &gt; 0</c> ve birim Piece değilse → kg.
    /// 5. Heuristik (son çare): isimde "KG" + uygun kategori ipucu → kg.
    /// 6. Aksi halde → adet.
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

            // 1) Admin/senkron tarafından AÇIKÇA işaretlenmiş ürünlere koşulsuz güven.
            //    Bool alanı varsayılan 'false' olduğundan 'true' güçlü bir pozitif sinyaldir
            //    (biri bilerek set etmiştir); 'false' ise zayıftır (hiç set edilmemiş olabilir).
            if (product.IsWeightBased)
            {
                return true;
            }

            // 2) Paketli ürün negatif sinyali, yapısal tahminleri (3-4. adım) EZER.
            //    NEDEN: Senkron/import sırasında yanlış doldurulmuş WeightUnit veya PricePerUnit,
            //    "ZEYTİN 500 GR" gibi paketli bir ürünü hatalıca kg'ya çevirmesin.
            if (WeightBasedProductRules.HasPackagedNameSignal(product.Name))
            {
                return false;
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

            // 5) Yapısal veri yetersiz: mevcut isim+kategori heuristiğine düş (kural çoğaltma yok).
            //    Kategori navigasyonu yüklenmemişse null geçilir; heuristik güvenli biçimde 'false' döner.
            return WeightBasedProductRules.IsVariableWeightKgProduct(
                product.Name,
                product.WeightUnit,
                product.Category?.Name);
        }

        /// <summary>
        /// Ürünün geçerli birim fiyatını döndürür.
        /// kg ürünlerde TL/kg fiyatı (<c>PricePerUnit</c>) önceliklidir; tanımlı değilse indirimli/normal
        /// fiyata düşer. Adet ürünlerde indirimli fiyat varsa o, yoksa normal fiyat kullanılır.
        /// Bu mantık daha önce PricingEngine ve OrderManager içinde kopyalanmıştı; tek noktaya alındı.
        /// </summary>
        public static decimal ResolveUnitPrice(Product? product, bool isWeightBased)
        {
            if (product is null)
            {
                return 0m;
            }

            if (isWeightBased)
            {
                return product.PricePerUnit > 0m
                    ? product.PricePerUnit
                    : (product.SpecialPrice ?? product.Price);
            }

            return product.SpecialPrice ?? product.Price;
        }

        /// <summary>
        /// Tespit ve birim fiyatı tek çağrıda döndüren kolaylık metodu.
        /// Çağıranların iki ayrı metodu yanlış sırada kullanma riskini azaltır.
        /// </summary>
        public static (bool IsWeightBased, decimal UnitPrice) Resolve(Product? product)
        {
            var isWeightBased = ResolveIsWeightBased(product);
            return (isWeightBased, ResolveUnitPrice(product, isWeightBased));
        }

        /// <summary>
        /// Birim, kütle bazlı (değişken ağırlıklı satışa uygun) mı?
        /// Kilogram ve Gram kütle birimidir; Liter/Milliliter (hacim) ve Piece (adet) hariçtir.
        /// </summary>
        private static bool IsMassWeightUnit(WeightUnit unit) =>
            unit is WeightUnit.Kilogram or WeightUnit.Gram;
    }
}
