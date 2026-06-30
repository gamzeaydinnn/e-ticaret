using System.Text.RegularExpressions;
using ECommerce.Entities.Enums;

namespace ECommerce.Business.Helpers
{
    public static class WeightBasedProductRules
    {
        private static readonly Regex FixedWeightPattern =
            new(@"\b\d+(?:[.,]\d+)?\s*(GR|KG|LT|ML|CL|L)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex StandaloneKgPattern =
            new(@"\bKG\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly string[] VariableWeightCategoryHints =
        {
            "MANAV",
            "MEYVE",
            "SEBZE",
            "YESILLIK",
            "YEŞİLLİK",
            "OT",
            "KASAP",
            "ET",
            "TAVUK",
            "BALIK",
            "DENIZ",
            "DENİZ",
            "PEYNIR",
            "PEYNİR",
            "SARKUTERI",
            "ŞARKÜTERİ",
            "ZEYTIN",
            "ZEYTİN"
        };

        /// <summary>
        /// Senkron/import (Mikro) katmanı için kalıcı <c>Product.IsWeightBased</c> bayrağını TÜRETİR.
        ///
        /// <see cref="WeightBasedProductResolver"/>'dan farkı: mevcut (muhtemelen bayat) bayrağa GÜVENMEZ;
        /// kararı yalnızca kaynak sistemin yapısal verisinden (WeightUnit) ve üründen üretir. Böylece bir
        /// ürün Mikro'da kg→adet değişirse bayrak doğru biçimde 'false'a güncellenir.
        ///
        /// NEDEN GEREKLİ: Eski popülasyon yalnız isim-heuristiğini (<see cref="IsVariableWeightKgProduct"/>)
        /// kullanıyordu; Mikro birimi KG (WeightUnit=Kilogram) olsa bile adında "KG" token'ı geçmeyen
        /// ürünleri yanlışlıkla 'adet' işaretliyor ve PricePerUnit'i 0 bırakıyordu.
        ///
        /// Kural: Paketli isim sinyali ("500 GR"/"ADET") yoksa VE (kütle birimi Kilogram/Gram VEYA
        /// isim+kategori heuristiği doğruysa) → tartılı (kg).
        /// </summary>
        public static bool DeriveIsWeightBasedForSync(
            string? productName,
            WeightUnit weightUnit,
            string? categoryNameOrCode)
        {
            if (HasPackagedNameSignal(productName))
            {
                return false;
            }

            var isMassUnit = weightUnit is WeightUnit.Kilogram or WeightUnit.Gram;
            return isMassUnit
                || IsVariableWeightKgProduct(productName, weightUnit, categoryNameOrCode);
        }

        /// <summary>
        /// İsimde sabit gramaj/hacim ("500 GR", "1 LT") veya "ADET" geçiyor mu?
        /// true → ürün paketli/adet bazlıdır ve değişken ağırlıklı (kg) DEĞİLDİR.
        ///
        /// NEDEN: <see cref="WeightBasedProductResolver"/>, yanlış set edilmiş WeightUnit/PricePerUnit
        /// gibi yapısal alanların paketli bir ürünü kg'ya çevirmesini engellemek için bu negatif
        /// sinyali yapısal tahminlerden ÖNCE kullanır. Regex'ler tek noktada (bu sınıfta) tutulduğundan
        /// kural çoğaltması (spagetti) önlenir.
        /// </summary>
        public static bool HasPackagedNameSignal(string? productName)
        {
            var normalizedName = (productName ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(normalizedName))
            {
                return false;
            }

            return FixedWeightPattern.IsMatch(normalizedName)
                || Regex.IsMatch(normalizedName, @"\bADET\b", RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// İsim + birim + kategori bilgisine dayalı HEURİSTİK kg tespiti.
        ///
        /// ⚠️ KULLANIM: Çalışma zamanında kg tespiti için doğrudan bu metodu ÇAĞIRMAYIN.
        /// Bunun yerine tek doğruluk kaynağı olan <see cref="WeightBasedProductResolver"/> kullanın;
        /// o, açık yapısal verilere (IsWeightBased/WeightUnit/PricePerUnit) öncelik verir ve yalnızca
        /// yapısal veri yoksa SON ÇARE olarak bu heuristiğe düşer.
        ///
        /// Bu metodun meşru doğrudan kullanım yerleri yalnızca: (1) <see cref="WeightBasedProductResolver"/>
        /// içindeki fallback, (2) Mikro senkron/import katmanının <c>Product.IsWeightBased</c> bayrağını
        /// ilk kez doldurması (popülasyon).
        /// </summary>
        public static bool IsVariableWeightKgProduct(
            string? productName,
            WeightUnit weightUnit,
            string? categoryNameOrCode)
        {
            var normalizedName = (productName ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(normalizedName))
            {
                return false;
            }

            if (HasPackagedNameSignal(productName))
            {
                return false;
            }

            var normalizedCategory = (categoryNameOrCode ?? string.Empty).Trim().ToUpperInvariant();
            if (!string.IsNullOrWhiteSpace(normalizedCategory)
                && VariableWeightCategoryHints.Any(h => normalizedCategory.Contains(h)))
            {
                // Meyve/sebze/kasap gibi kategorilerde isimde "KG" olmasa da tartılı satılır.
                return true;
            }

            if (!StandaloneKgPattern.IsMatch(normalizedName))
            {
                return false;
            }

            if (weightUnit == WeightUnit.Kilogram)
            {
                return true;
            }

            if (weightUnit != WeightUnit.Piece)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(normalizedCategory))
            {
                return false;
            }

            return VariableWeightCategoryHints.Any(normalizedCategory.Contains);
        }
    }
}
