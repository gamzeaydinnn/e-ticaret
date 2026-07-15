using System.Globalization;
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

        /// <summary>
        /// Açık tartımla satılan kategori ipuçları (isimde "KG" olmasa da kg).
        /// </summary>
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
            "ZEYTİN",
            // Kuruyemiş / bakliyat (açık satılan)
            "KURUYEMIS",
            "KURUYEMİŞ",
            "KURU YEMIS",
            "KURU YEMİŞ",
            "BAKLIYAT",
            "BAKLİYAT",
            "BAHARAT",
        };

        /// <summary>
        /// Açık kg/gr ile satılan ürün adı anahtarları (paketli "250 GR" hariç).
        /// Mikro birimi ADET olsa bile badem/fındık vb. tartılı ürün sayılır.
        /// </summary>
        private static readonly string[] VariableWeightNameHints =
        {
            "BADEM",
            "FINDIK",
            "FINDİK",
            "FISTIK",
            "FISTİK",
            "FISTIG",
            "FISTIĞ",
            "CEVIZ",
            "CEVİZ",
            "KAJU",
            "ANTEP",
            "LEBLEBI",
            "LEBLEBİ",
            "KABAK CEKIRDEK",
            "KABAK ÇEKİRDEK",
            "AY CEKIRDEK",
            "AY ÇEKİRDEK",
            "CEKIRDEK",
            "ÇEKİRDEK",
            "KURU UZUM",
            "KURU ÜZÜM",
            "KURU INCIR",
            "KURU İNCİR",
            "KURU KAYISI",
            "HURMA",
            "KIRMIZI PUL",
            "TOZ BIBER",
            "TOZ BİBER",
            "KIMYON",
            "KİMYON",
            "SUMAC",
            "SUMÁK",
            "SUMAK",
            "NANE",
            "REZEN",
            "NOHUT",
            "MERCIMEK",
            "MERCİMEK",
            "FASULYE",
            "BÖRÜLCE",
            "BULGUR",
            "PIRINC",
            "PİRİNÇ",
            "IRMIK",
            "İRMİK",
        };

        /// <summary>
        /// Senkron/import (Mikro) katmanı için kalıcı <c>Product.IsWeightBased</c> bayrağını TÜRETİR.
        ///
        /// <see cref="WeightBasedProductResolver"/>'dan farkı: mevcut (muhtemelen bayat) bayrağa GÜVENMEZ;
        /// kararı yalnızca kaynak sistemin yapısal verisinden (WeightUnit) ve üründen üretir. Böylece bir
        /// ürün Mikro'da kg→adet değişirse bayrak doğru biçimde 'false'a güncellenir.
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
        /// Çalışma zamanı için <see cref="WeightBasedProductResolver"/> kullanın.
        /// </summary>
        public static bool IsVariableWeightKgProduct(
            string? productName,
            WeightUnit weightUnit,
            string? categoryNameOrCode)
        {
            var normalizedName = NormalizeForHintMatch(productName);
            if (string.IsNullOrWhiteSpace(normalizedName))
            {
                return false;
            }

            if (HasPackagedNameSignal(productName))
            {
                return false;
            }

            var normalizedCategory = NormalizeForHintMatch(categoryNameOrCode);
            if (!string.IsNullOrWhiteSpace(normalizedCategory)
                && VariableWeightCategoryHints.Any(h =>
                    normalizedCategory.Contains(NormalizeForHintMatch(h))))
            {
                // Meyve/sebze/kasap/kuruyemiş gibi kategorilerde isimde "KG" olmasa da tartılı satılır.
                return true;
            }

            // Badem, fındık vb. — kategori "Atıştırmalık" olsa da açık tartımlıdır.
            if (VariableWeightNameHints.Any(h =>
                    normalizedName.Contains(NormalizeForHintMatch(h))))
            {
                return true;
            }

            if (!StandaloneKgPattern.IsMatch(productName ?? string.Empty))
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

            return VariableWeightCategoryHints.Any(h =>
                normalizedCategory.Contains(NormalizeForHintMatch(h)));
        }

        /// <summary>
        /// Türkçe karakterleri karşılaştırma için normalize eder (İ→I, Ş→S, …).
        /// </summary>
        private static string NormalizeForHintMatch(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var upper = value.Trim().ToUpper(CultureInfo.GetCultureInfo("tr-TR"));
            return upper
                .Replace('İ', 'I')
                .Replace('Ş', 'S')
                .Replace('Ğ', 'G')
                .Replace('Ü', 'U')
                .Replace('Ö', 'O')
                .Replace('Ç', 'C');
        }
    }
}
