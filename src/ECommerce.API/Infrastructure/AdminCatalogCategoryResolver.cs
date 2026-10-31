using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ECommerce.Core.Helpers;
using ECommerce.Entities.Concrete;

namespace ECommerce.API.Infrastructure
{
    internal static class AdminCatalogCategoryResolver
    {
        private static readonly Dictionary<string, string> CategorySlugAliases = new(StringComparer.OrdinalIgnoreCase)
        {
            ["sut-urunleri"] = "sut-ve-sut-urunleri",
            ["meyve-sebze"] = "meyve-ve-sebze",
            ["et-tavuk"] = "et-ve-et-urunleri",
            ["et-tavuk-balik"] = "et-ve-et-urunleri",
            ["dondurulmus-gida"] = "dondurma-ve-dondurulmus-gida",
            ["dondurma-dondurulmus"] = "dondurma-ve-dondurulmus-gida"
        };

        private static readonly Dictionary<string, string[]> ProductNameCategoryHints = new(StringComparer.OrdinalIgnoreCase)
        {
            ["et-ve-et-urunleri"] = new[]
            {
                "sucuk", "salam", "sosis", "pastirma", "pastırma", "kavurma",
                "jambon", "füme et", "fume et", "dana", "kuzu", "köfte", "kofte",
                "kıyma", "kiyma", "antrikot", "bonfile", "biftek", "tavuk", "hindi"
            },
            ["temel-gida"] = new[] { "recel", "reçel", "pekmez", "marmelat", "bal", "tahin" },
            ["dondurma-ve-dondurulmus-gida"] = new[] { "dondurma", "donmus", "donmuş", "dondurulmus", "dondurulmuş", "superfresh", "super fresh" },
            ["ev-ve-mutfak"] = new[] { "pisirme kagidi", "pişirme kağıdı", "kagit", "kağıt", "servis seti", "servis", "folyo" },
            ["temizlik"] = new[] { "eldiven", "muayene" },
            ["sut-ve-sut-urunleri"] = new[]
            {
                "nesquik", "süt", "sut", "yoğurt", "yogurt", "ayran", "kefir",
                "peynir", "kaşar", "kasar", "labne", "tereyağ", "tereyag",
                "krema", "kaymak", "hindistan cevizi sütü", "hindistan cevizi sutu",
                "bitkisel süt", "bitkisel sut"
            },
        };

        private const string UncategorizedCategorySlug = "diger";

        public static string NormalizeCategorySlug(string? slug)
        {
            if (string.IsNullOrWhiteSpace(slug))
            {
                return string.Empty;
            }

            var normalized = GenerateSlug(slug);
            return CategorySlugAliases.TryGetValue(normalized, out var alias)
                ? alias
                : normalized;
        }

        public static (int? CategoryId, string CategorySlug, string CategoryName) ResolveCategoryInfo(
            string? anagrupKod,
            string? grupKod,
            string? productName,
            Dictionary<string, List<MikroCategoryMapping>> mappings,
            Dictionary<int, string> idToSlug,
            Dictionary<string, string> slugToName)
        {
            var categoryId = ResolveCategoryId(anagrupKod, grupKod, mappings);
            if (!categoryId.HasValue)
            {
                return TryResolveCategoryInfoFromProductName(productName, idToSlug, slugToName);
            }

            if (!idToSlug.TryGetValue(categoryId.Value, out var slug))
            {
                return (categoryId, string.Empty, string.Empty);
            }

            var normalizedSlug = NormalizeCategorySlug(slug);
            if (string.Equals(normalizedSlug, UncategorizedCategorySlug, StringComparison.OrdinalIgnoreCase))
            {
                var fallback = TryResolveCategoryInfoFromProductName(productName, idToSlug, slugToName);
                if (fallback.CategoryId.HasValue)
                {
                    return fallback;
                }
            }

            return (
                categoryId,
                normalizedSlug,
                slugToName.GetValueOrDefault(slug, slugToName.GetValueOrDefault(normalizedSlug, string.Empty)));
        }

        private static int? ResolveCategoryId(
            string? anagrupKod,
            string? grupKod,
            Dictionary<string, List<MikroCategoryMapping>> mappings)
        {
            var normAna = MikroGrupKodHelper.NormalizeAnaKod(anagrupKod);
            var normAlt = MikroGrupKodHelper.NormalizeAltKod(grupKod);

            if (!string.IsNullOrWhiteSpace(normAna) &&
                mappings.TryGetValue(normAna, out var directMappings) &&
                directMappings.Count > 0)
            {
                if (!string.IsNullOrWhiteSpace(normAlt))
                {
                    var exactAltgrupMatch = directMappings.FirstOrDefault(mapping =>
                        !string.IsNullOrWhiteSpace(mapping.MikroAltgrupKod) &&
                        string.Equals(
                            MikroGrupKodHelper.NormalizeAltKod(mapping.MikroAltgrupKod),
                            normAlt,
                            StringComparison.OrdinalIgnoreCase));

                    if (exactAltgrupMatch != null)
                    {
                        return exactAltgrupMatch.CategoryId;
                    }
                }

                var baseMapping = directMappings.FirstOrDefault(mapping =>
                    string.IsNullOrWhiteSpace(mapping.MikroAltgrupKod) &&
                    string.IsNullOrWhiteSpace(mapping.MikroMarkaKod));

                return (baseMapping ?? directMappings[0]).CategoryId;
            }

            if (!string.IsNullOrWhiteSpace(normAlt) &&
                mappings.TryGetValue(normAlt, out var groupMappings) &&
                groupMappings.Count > 0)
            {
                return groupMappings[0].CategoryId;
            }

            if (mappings.TryGetValue("*", out var wildcardMappings) && wildcardMappings.Count > 0)
            {
                return wildcardMappings[0].CategoryId;
            }

            return null;
        }

        private static (int? CategoryId, string CategorySlug, string CategoryName) TryResolveCategoryInfoFromProductName(
            string? productName,
            Dictionary<int, string> idToSlug,
            Dictionary<string, string> slugToName)
        {
            if (string.IsNullOrWhiteSpace(productName))
            {
                return (null, string.Empty, string.Empty);
            }

            var normalizedName = NormalizeHintText(productName);

            foreach (var (slug, hints) in ProductNameCategoryHints)
            {
                if (!hints.Any(hint => normalizedName.Contains(NormalizeHintText(hint), StringComparison.Ordinal)))
                {
                    continue;
                }

                var categoryEntry = idToSlug.FirstOrDefault(item =>
                    string.Equals(item.Value, slug, StringComparison.OrdinalIgnoreCase));

                if (categoryEntry.Key <= 0)
                {
                    continue;
                }

                return (
                    categoryEntry.Key,
                    slug,
                    slugToName.GetValueOrDefault(slug, string.Empty));
            }

            return (null, string.Empty, string.Empty);
        }

        private static string NormalizeHintText(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var normalized = Regex.Replace(value.ToLowerInvariant(), @"[^\p{L}\p{Nd}]+", " ");
            normalized = Regex.Replace(normalized, @"\s+", " ").Trim();
            return $" {normalized} ";
        }

        private static string GenerateSlug(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return string.Empty;
            }

            var lower = input.Trim().ToLowerInvariant();
            lower = lower
                .Replace("ç", "c").Replace("ğ", "g").Replace("ı", "i").Replace("ö", "o").Replace("ş", "s").Replace("ü", "u")
                .Replace("Ç", "c").Replace("Ğ", "g").Replace("İ", "i").Replace("Ö", "o").Replace("Ş", "s").Replace("Ü", "u");
            lower = Regex.Replace(lower, @"[^a-z0-9\s-]", string.Empty);
            lower = Regex.Replace(lower, @"\s+", "-");
            lower = Regex.Replace(lower, "-+", "-");
            return lower.Trim('-');
        }
    }
}
