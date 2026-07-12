using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ECommerce.Core.Helpers
{
    /// <summary>
    /// Vitrin ürün araması: Türkçe normalizasyon, kelime/önek eşleşmesi ve alaka skoru.
    /// Örnek: "el" → "Elma", "su" → "Kaynak Suyu" (tam/türev önde, "Sucuk" geride).
    /// </summary>
    public static class ProductSearchMatcher
    {
        private static readonly Regex NonAlphaNumeric = new(@"[^a-z0-9\s-]", RegexOptions.Compiled);
        private static readonly Regex MultiSpace = new(@"[\s]+", RegexOptions.Compiled);
        private static readonly Regex MultiDash = new(@"-+", RegexOptions.Compiled);
        private static readonly Regex VolumeHint = new(@"\d+-?(?:l|lt|ml|litre|cl)\b", RegexOptions.Compiled);

        /// <summary>
        /// Sık aranan ürün tipleri için bilinen türevler (su → suyu).
        /// </summary>
        private static readonly Dictionary<string, string[]> RelatedLemmas = new(StringComparer.Ordinal)
        {
            ["su"] = new[] { "su", "suyu", "sular" },
            ["soda"] = new[] { "soda", "sodasi", "sodalar" },
            ["et"] = new[] { "et", "eti", "etler" },
            ["sut"] = new[] { "sut", "sutu", "sutler", "sutlu" },
            ["elma"] = new[] { "elma", "elmasi", "elmalar" },
            ["ekmek"] = new[] { "ekmek", "ekmegi", "ekmekler" },
            ["peynir"] = new[] { "peynir", "peyniri", "peynirler" },
            ["yogurt"] = new[] { "yogurt", "yogurdu", "yogurtlar" },
            ["cay"] = new[] { "cay", "cayi", "caylar" },
            ["kahve"] = new[] { "kahve", "kahvesi", "kahveler" },
            ["yag"] = new[] { "yag", "yagi", "yaglar" },
            ["pirinc"] = new[] { "pirinc", "pirinci" },
            ["makarna"] = new[] { "makarna", "makarnasi" },
        };

        private static readonly string[] BeverageCategoryHints =
        {
            "icecek", "su", "soda", "gazoz", "maden", "mesrubat", "su-ve-icecek", "sular"
        };

        private static readonly string[] DrinkingWaterBoostHints =
        {
            "icme", "kaynak", "maden", "damacana", "dogal", "mineralli", "hayat", "erikli",
            "pinar", "saka", "hamidiye", "nestle", "pure-life", "aqua", "spas"
        };

        private static readonly string[] NonBeveragePenaltyHints =
        {
            "koruma", "spf", "gunes", "guneskoruyucu", "krem", "losyon", "serum", "sampuan",
            "sabun", "ferahlik", "makyaj", "cilt", "nemlendirici", "deodorant", "parfum",
            "dis-macunu", "bebek-bez", "islak-mendil"
        };

        public static bool Matches(
            string? name,
            string? description,
            string? categoryName,
            string? sku,
            string? query,
            string? brand = null)
        {
            return Score(name, description, categoryName, sku, query, brand) > 0;
        }

        /// <summary>
        /// Alaka skoru. 0 = eşleşme yok. Yüksek skor = daha iyi eşleşme.
        /// </summary>
        public static int Score(
            string? name,
            string? description,
            string? categoryName,
            string? sku,
            string? query,
            string? brand = null)
        {
            var queryTokens = Tokenize(query);
            if (queryTokens.Count == 0)
                return 0;

            var total = 0;
            foreach (var queryToken in queryTokens)
            {
                var best = 0;
                best = Math.Max(best, ScoreTokenInField(name, queryToken, 100, preferLeadingToken: true));
                best = Math.Max(best, ScoreTokenInField(brand, queryToken, 80, preferLeadingToken: true));
                best = Math.Max(best, ScoreTokenInField(sku, queryToken, 70, preferLeadingToken: false));
                best = Math.Max(best, ScoreTokenInField(categoryName, queryToken, 45, preferLeadingToken: false));
                best = Math.Max(best, ScoreTokenInField(description, queryToken, 25, preferLeadingToken: false));

                if (best == 0)
                    return 0;

                total += best;
            }

            var nameSlug = Normalize(name);
            var categorySlug = Normalize(categoryName);
            var fullQuery = string.Join("-", queryTokens);

            if (!string.IsNullOrEmpty(nameSlug))
            {
                if (string.Equals(nameSlug, fullQuery, StringComparison.Ordinal))
                    total += 60;
                else if (nameSlug.StartsWith(fullQuery + "-", StringComparison.Ordinal) ||
                         nameSlug.StartsWith(fullQuery, StringComparison.Ordinal))
                    total += 35;
            }

            total += ScoreIntentBoosts(queryTokens, nameSlug, categorySlug);
            return Math.Max(0, total);
        }

        private static int ScoreIntentBoosts(
            IReadOnlyList<string> queryTokens,
            string nameSlug,
            string categorySlug)
        {
            var boost = 0;
            var isWaterQuery = queryTokens.Any(t => t is "su" or "suyu" or "soda");
            if (!isWaterQuery)
                return 0;

            if (BeverageCategoryHints.Any(h =>
                    categorySlug.Equals(h, StringComparison.Ordinal) ||
                    categorySlug.Contains(h, StringComparison.Ordinal)))
            {
                boost += 55;
            }

            if (DrinkingWaterBoostHints.Any(h => nameSlug.Contains(h, StringComparison.Ordinal)))
                boost += 40;

            if (VolumeHint.IsMatch(nameSlug))
                boost += 25;

            if (nameSlug.Contains("icme-suyu", StringComparison.Ordinal) ||
                nameSlug.Contains("kaynak-suyu", StringComparison.Ordinal) ||
                nameSlug.Contains("maden-suyu", StringComparison.Ordinal) ||
                Regex.IsMatch(nameSlug, @"(^|-)su(-\d|-l|-lt|-ml|$)"))
            {
                boost += 50;
            }

            if (NonBeveragePenaltyHints.Any(h => nameSlug.Contains(h, StringComparison.Ordinal)))
                boost -= 120;

            if (categorySlug.Contains("temizlik", StringComparison.Ordinal) ||
                categorySlug.Contains("kisisel", StringComparison.Ordinal) ||
                categorySlug.Contains("kozmetik", StringComparison.Ordinal) ||
                categorySlug.Contains("bakim", StringComparison.Ordinal))
            {
                boost -= 80;
            }

            return boost;
        }

        private static int ScoreTokenInField(
            string? value,
            string queryToken,
            int weight,
            bool preferLeadingToken)
        {
            if (string.IsNullOrWhiteSpace(value) || string.IsNullOrEmpty(queryToken))
                return 0;

            var slug = Normalize(value);
            if (string.IsNullOrEmpty(slug))
                return 0;

            var tokens = slug.Split('-', StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length == 0)
                return 0;

            var lemmas = GetLemmas(queryToken);

            // 1) Tam kelime veya bilinen türev (su ↔ suyu, elma ↔ elmasi)
            if (tokens.Any(t => lemmas.Contains(t)))
            {
                if (tokens.Any(t => string.Equals(t, queryToken, StringComparison.Ordinal)))
                    return weight;
                return (int)(weight * 0.95);
            }

            // 2) Önek: "el" → "elma", "pey" → "peynir", "su" → "sucuk" (düşük skor)
            var prefixHits = tokens
                .Where(t => t.StartsWith(queryToken, StringComparison.Ordinal) && t.Length >= queryToken.Length)
                .ToList();

            if (prefixHits.Count > 0)
            {
                // En kısa tamamlanma = en yakın ürün adı ("el" için "elma" > "elektronik")
                var bestToken = prefixHits.OrderBy(t => t.Length).ThenBy(t => t, StringComparer.Ordinal).First();
                var completeness = (double)queryToken.Length / bestToken.Length;
                var isLeading = preferLeadingToken &&
                                tokens[0].StartsWith(queryToken, StringComparison.Ordinal);

                // Tam eşitlik zaten yukarıda; burada sadece önek
                if (bestToken.Length == queryToken.Length)
                    return weight;

                var prefixWeight = 0.50 + (0.35 * completeness) + (isLeading ? 0.12 : 0);
                return (int)(weight * Math.Min(0.92, prefixWeight));
            }

            // 3) Alt dizgi — yalnızca 3+ karakter (kısa sorguda gürültü olmasın)
            if (queryToken.Length >= 3 && slug.Contains(queryToken, StringComparison.Ordinal))
                return (int)(weight * 0.35);

            return 0;
        }

        private static HashSet<string> GetLemmas(string queryToken)
        {
            var set = new HashSet<string>(StringComparer.Ordinal) { queryToken };
            if (RelatedLemmas.TryGetValue(queryToken, out var related))
            {
                foreach (var item in related)
                    set.Add(item);
            }

            return set;
        }

        public static IReadOnlyList<string> Tokenize(string? text)
        {
            var normalized = Normalize(text);
            if (string.IsNullOrEmpty(normalized))
                return Array.Empty<string>();

            return normalized
                .Split('-', StringSplitOptions.RemoveEmptyEntries)
                .ToArray();
        }

        public static string Normalize(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            var slug = text.Trim().ToLowerInvariant()
                .Replace("ı", "i").Replace("ğ", "g").Replace("ü", "u")
                .Replace("ş", "s").Replace("ö", "o").Replace("ç", "c")
                .Replace("İ", "i").Replace("Ğ", "g").Replace("Ü", "u")
                .Replace("Ş", "s").Replace("Ö", "o").Replace("Ç", "c");

            slug = NonAlphaNumeric.Replace(slug, "");
            slug = MultiSpace.Replace(slug, "-");
            slug = MultiDash.Replace(slug, "-");
            return slug.Trim('-');
        }
    }
}
