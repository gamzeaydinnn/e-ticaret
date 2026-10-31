using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using ECommerce.Entities.Enums;

namespace ECommerce.Business.Helpers
{
    public static class WeightBasedProductRules
    {
        private static readonly Regex FixedWeightPattern =
            new(@"\b\d+(?:[.,]\d+)?\s*(GR|G|GRAM|KG|LT|ML|CL|L)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex StandaloneKgPattern =
            new(@"\bKG\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex PackagedPieceNamePattern =
            new(@"\b(ADET|AD|PAKET|PK|DEMET|BAG|BAĞ|KUTU|KAVANOZ|TENEKE|POSET|POŞET|SISE|ŞİŞE|KOLI|KOLİ|TABAK|FILE|FİLE)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex CannedOrFrozenNamePattern =
            new(@"\b(KONSERVE|KONSERVESI|KONSERVESİ|DONUK|DONDURULMUS|DONDURULMUŞ|DONDURMA|TURSU|TURŞU|GARNITUR|GARNİTÜR|KOZLENMIS|KÖZLENMİŞ|HASLANMIS|HAŞLANMIŞ|SUPERFRESH|FEAST)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// Konserve, dondurulmuş/donuk gıda, dondurma, turşu ve paketli alt kategorilerde
        /// kesinlikle kg (açık tartım) satışı yoktur; bu ürünler paket/adet ile satılır.
        /// </summary>
        private static readonly string[] NonWeightCategoryHints =
        {
            "KONSERVE",
            "TURSU",
            "TURŞU",
            "DONUK",
            "DONDURULMUS",
            "DONDURULMUŞ",
            "DONDURMA",
            "PAKETLI",
            "PAKETLİ",
            "PAKET KURUYEMIS",
            "PAKET KURUYEMİŞ",
        };

        private static readonly HashSet<string> NonWeightCategoryCodes = new(StringComparer.OrdinalIgnoreCase)
        {
            "2203", // Konserve Meyve & Sebze
            "2204", // Donuk Meyve & Sebze
            "703",  // Konserve & Turşu
            "1102", // Paket Kuruyemiş
            "1300", // Dondurulmuş Gıda (Ana Grup)
            "1301", // Dondurulmuş Balık & Deniz Ürünleri
            "1302", // Dondurulmuş Piliç Ürünleri
            "1303", // Dondurulmuş Et Ürünleri
            "1304", // Dondurulmuş Sebze & Meyve
            "1305", // Dondurulmuş Pasta & Unlu Mamuller
            "1306", // Diğer Dondurulmuş Gıda
            "800",  // Dondurma (Ana Grup)
            "801",  // Kap Dondurma
            "802",  // Tek Dondurma
        };

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
            "KASAP",
            "KIRMIZI ET",
            "BEYAZ ET",
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
            "ACIK KURUYEMIS",
            "AÇIK KURUYEMİŞ",
            "KURUYEMIS",
            "KURUYEMİŞ",
            "KURU YEMIS",
            "KURU YEMİŞ",
            "BAKLIYAT",
            "BAKLİYAT",
            "BAHARAT",
        };

        /// <summary>
        /// Açık kg/gr ile satılan ürün adı anahtarları (paketli "250 GR", konserve ve donuk hariç).
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
        /// </summary>
        public static bool DeriveIsWeightBasedForSync(
            string? productName,
            WeightUnit weightUnit,
            string? categoryNameOrCode)
        {
            if (HasPackagedNameSignal(productName) || IsPackagedOrExcludedCategory(categoryNameOrCode))
            {
                return false;
            }

            var isMassUnit = weightUnit is WeightUnit.Kilogram or WeightUnit.Gram;
            return isMassUnit
                || IsVariableWeightKgProduct(productName, weightUnit, categoryNameOrCode);
        }

        /// <summary>
        /// İsimde sabit gramaj/hacim ("500 GR", "1 LT"), paket/adet birimi ("ADET", "PAKET", "KAVANOZ")
        /// veya konserve/dondurulmuş ("KONSERVE", "DONUK", "DONDURULMUŞ") geçiyor mu?
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
                || PackagedPieceNamePattern.IsMatch(normalizedName)
                || CannedOrFrozenNamePattern.IsMatch(normalizedName);
        }

        /// <summary>
        /// Kategori adı, slug veya Mikro grup kodu konserve, dondurulmuş/donuk gıda, dondurma veya paketli
        /// bir alt kategoriye mi ait? True dönerse ürün ASLA kg ile satılmaz.
        /// </summary>
        public static bool IsPackagedOrExcludedCategory(
            string? categoryNameOrCode,
            string? categorySlug = null,
            string? mikroAltGrupKod = null,
            string? mikroAnaGrupKod = null)
        {
            var codes = new[] { categoryNameOrCode, mikroAltGrupKod, mikroAnaGrupKod };
            foreach (var raw in codes)
            {
                var trimmed = (raw ?? string.Empty).Trim();
                if (!string.IsNullOrEmpty(trimmed) && NonWeightCategoryCodes.Contains(trimmed))
                {
                    return true;
                }
            }

            var combinedText = NormalizeForHintMatch(
                string.Join(" ", new[] { categoryNameOrCode, categorySlug }.Where(s => !string.IsNullOrWhiteSpace(s))));

            if (string.IsNullOrWhiteSpace(combinedText))
            {
                return false;
            }

            return NonWeightCategoryHints.Any(h =>
                combinedText.Contains(NormalizeForHintMatch(h)));
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

            if (HasPackagedNameSignal(productName) || IsPackagedOrExcludedCategory(categoryNameOrCode))
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
