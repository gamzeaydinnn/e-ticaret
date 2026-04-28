using ECommerce.Core.Interfaces.Mapping;
using ECommerce.Data.Context;
using ECommerce.Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ECommerce.Business.Services.Mapping
{
    /// <summary>
    /// Mikro grup kodlarını e-ticaret kategorileriyle otomatik eşleştiren motor.
    /// 
    /// NEDEN: Mikro ERP'den gelen binlerce ürünün her birini elle kategorilendirmek
    /// pratik değil. Bu motor Türkçe metin normalizasyonu + benzerlik skoru ile
    /// ürün gruplarını mevcut kategorilere eşler veya yeni kategori oluşturur.
    /// 
    /// ALGORİTMA:
    /// 1. Exact match (normalize edilmiş metinler birebir eşit mi?)
    /// 2. Contains match (biri diğerini içeriyor mu?)
    /// 3. Token-based Jaccard similarity (kelime bazlı kesişim oranı)
    /// 4. Eşik altında: AutoCreateCategories=true ise yeni kategori oluştur
    /// 5. Son çare: "Diğer" (wildcard) kategorisine at
    /// 
    /// NEDEN Levenshtein değil Jaccard:
    /// Levenshtein karakter bazlıdır ve "ET URUNLERI" vs "Et ve Et Ürünleri" gibi
    /// token sırası/ekstra kelime durumlarında düşük skor verir.
    /// Jaccard kelime bazlı çalıştığı için daha doğru sonuç verir.
    /// </summary>
    public class AutoCategoryMappingEngine : IAutoCategoryMappingEngine
    {
        private readonly ECommerceDbContext _context;
        private readonly IMikroCategoryMappingService _mappingService;
        private readonly ILogger<AutoCategoryMappingEngine> _logger;
        private readonly bool _autoCreateCategories;

        // Fuzzy match eşik değeri — bu skorun üstündeki eşleşmeler otomatik kabul edilir
        // NEDEN: 0.4 çok düşük — "ET" gibi kısa tokenlar false-positive üretiyordu
        // 0.65 ile sadece gerçekten benzer grup kodları eşleşir
        private const double AutoMatchThreshold = 0.65;

        // Türkçe karakter normalizasyon tablosu
        private static readonly Dictionary<char, char> TurkishCharMap = new()
        {
            ['ç'] = 'c', ['Ç'] = 'c',
            ['ğ'] = 'g', ['Ğ'] = 'g',
            ['ı'] = 'i', ['İ'] = 'i',
            ['ö'] = 'o', ['Ö'] = 'o',
            ['ş'] = 's', ['Ş'] = 's',
            ['ü'] = 'u', ['Ü'] = 'u'
        };

        // Eşlemede yok sayılacak dolgu kelimeleri — benzerlik skorunu bozmasın
        private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
        {
            "ve", "ile", "veya", "icin", "den", "dan", "urunleri", "urunler",
            "malzemeleri", "malzemeler", "cesitleri", "cesitler"
        };

        public AutoCategoryMappingEngine(
            ECommerceDbContext context,
            IMikroCategoryMappingService mappingService,
            ILogger<AutoCategoryMappingEngine> logger,
            IConfiguration configuration)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _mappingService = mappingService ?? throw new ArgumentNullException(nameof(mappingService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // appsettings'den kontrol — false ise yeni kategori oluşturmaz, "Diğer"e atar
            _autoCreateCategories = configuration.GetValue("CategoryMapping:AutoCreateCategories", true);
        }

        /// <inheritdoc />
        public async Task<List<CategorySuggestion>> SuggestMappingAsync(
            string anagrupKod,
            string? altgrupKod = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(anagrupKod))
                return new List<CategorySuggestion>();

            var categories = await _context.Categories
                .AsNoTracking()
                .Where(c => c.IsActive)
                .ToListAsync(cancellationToken);

            // Normalize edilmiş Mikro grup adı — karşılaştırma için hazırla
            var normalizedGroup = NormalizeTurkish(anagrupKod);
            var groupTokens = Tokenize(normalizedGroup);

            var suggestions = new List<CategorySuggestion>();

            foreach (var cat in categories)
            {
                var normalizedCat = NormalizeTurkish(cat.Name);
                var catTokens = Tokenize(normalizedCat);

                double score = 0;
                string matchType;

                // 1. Tam eşleşme (normalize edilmiş)
                if (normalizedGroup == normalizedCat)
                {
                    score = 1.0;
                    matchType = "exact";
                }
                // 2. İçerme kontrolü — biri diğerini içeriyor mu
                else if (normalizedCat.Contains(normalizedGroup) || normalizedGroup.Contains(normalizedCat))
                {
                    // Daha kısa olanın uzunluk oranı kadar skor ver
                    var shorter = Math.Min(normalizedGroup.Length, normalizedCat.Length);
                    var longer = Math.Max(normalizedGroup.Length, normalizedCat.Length);
                    score = (double)shorter / longer;
                    score = Math.Max(score, 0.6); // Contains her zaman minimum 0.6
                    matchType = "contains";
                }
                // 3. Token-based Jaccard similarity
                else
                {
                    score = CalculateJaccardSimilarity(groupTokens, catTokens);
                    matchType = "fuzzy";
                }

                if (score > 0.1) // Çok düşük skorları atla
                {
                    suggestions.Add(new CategorySuggestion
                    {
                        CategoryId = cat.Id,
                        CategoryName = cat.Name,
                        CategorySlug = cat.Slug,
                        Score = Math.Round(score, 3),
                        MatchType = matchType
                    });
                }
            }

            return suggestions
                .OrderByDescending(s => s.Score)
                .Take(5)
                .ToList();
        }

        /// <inheritdoc />
        public async Task<int> ResolveOrCreateMappingAsync(
            string anagrupKod,
            string? altgrupKod = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(anagrupKod))
                return await GetDigerCategoryIdAsync(cancellationToken);

            // 1. Mevcut mapping var mı?
            var existing = await _mappingService.GetMappingAsync(
                anagrupKod, altgrupKod, null, cancellationToken);

            if (existing != null)
                return existing.CategoryId;

            // 2. Fuzzy match ile mevcut kategoriye eşleşiyor mu?
            var suggestions = await SuggestMappingAsync(anagrupKod, altgrupKod, cancellationToken);
            var bestMatch = suggestions.FirstOrDefault();

            if (bestMatch != null && bestMatch.Score >= AutoMatchThreshold)
            {
                // Otomatik mapping oluştur
                await CreateMappingAsync(anagrupKod, altgrupKod, bestMatch.CategoryId,
                    $"Auto-mapped ({bestMatch.MatchType}, score={bestMatch.Score})", cancellationToken);

                _logger.LogInformation(
                    "[AutoMapping] '{AnagrupKod}' → '{Category}' (score={Score}, type={Type})",
                    anagrupKod, bestMatch.CategoryName, bestMatch.Score, bestMatch.MatchType);

                return bestMatch.CategoryId;
            }

            // 3. Yeni kategori oluştur (config izin veriyorsa)
            // ADIM 10: Hiyerarşi desteği — AnagrupKod → Ana kategori, AltgrupKod → Alt kategori
            if (_autoCreateCategories)
            {
                var newCategoryId = await CreateHierarchicalCategoryAsync(
                    anagrupKod, altgrupKod, cancellationToken);

                _logger.LogInformation(
                    "[AutoMapping] Kategori oluşturuldu: '{AnagrupKod}/{AltgrupKod}' → CategoryId={CategoryId}",
                    anagrupKod, altgrupKod ?? "(yok)", newCategoryId);

                return newCategoryId;
            }

            // 4. "Diğer" kategorisine at
            var digerId = await GetDigerCategoryIdAsync(cancellationToken);

            await CreateMappingAsync(anagrupKod, altgrupKod, digerId,
                "Auto-mapped → Diğer (eşleşme bulunamadı, AutoCreateCategories=false)", cancellationToken);

            _logger.LogWarning(
                "[AutoMapping] '{AnagrupKod}' eşlenemedi, 'Diğer' kategorisine atandı", anagrupKod);

            return digerId;
        }

        /// <inheritdoc />
        public async Task<AutoMappingResult> DiscoverAndMapAllAsync(
            CancellationToken cancellationToken = default)
        {
            var result = new AutoMappingResult();

            _logger.LogInformation("[AutoMapping] Toplu keşif + eşleme başlıyor...");

            // ADIM 10: AnagrupKod bazlı ana grupları keşfet
            var groupCodes = await _context.MikroProductCaches
                .AsNoTracking()
                .Where(c => !string.IsNullOrEmpty(c.AnagrupKod))
                .GroupBy(c => c.AnagrupKod)
                .Select(g => new { AnagrupKod = g.Key!, ProductCount = g.Count() })
                .ToListAsync(cancellationToken);

            // ADIM 10: AnagrupKod+GrupKod (altgrup) combo'larını da keşfet (hiyerarşi için)
            var subGroupCodes = await _context.MikroProductCaches
                .AsNoTracking()
                .Where(c => !string.IsNullOrEmpty(c.AnagrupKod) && !string.IsNullOrEmpty(c.GrupKod))
                .GroupBy(c => new { c.AnagrupKod, c.GrupKod })
                .Select(g => new { AnagrupKod = g.Key.AnagrupKod!, AltgrupKod = g.Key.GrupKod!, ProductCount = g.Count() })
                .ToListAsync(cancellationToken);

            result.TotalGroupCodes = groupCodes.Count;

            // Mevcut eşlemeleri yükle — tekrar eşleme yapma
            var existingMappings = await _context.Set<MikroCategoryMapping>()
                .AsNoTracking()
                .Select(m => m.MikroAnagrupKod.ToUpper())
                .ToListAsync(cancellationToken);

            var mappedSet = new HashSet<string>(existingMappings, StringComparer.OrdinalIgnoreCase);

            foreach (var group in groupCodes)
            {
                if (mappedSet.Contains(group.AnagrupKod))
                {
                    result.AlreadyMapped++;
                    continue;
                }

                try
                {
                    var suggestions = await SuggestMappingAsync(group.AnagrupKod, null, cancellationToken);
                    var bestMatch = suggestions.FirstOrDefault();

                    int categoryId;
                    string matchType;
                    double score;
                    string categoryName;

                    if (bestMatch != null && bestMatch.Score >= AutoMatchThreshold)
                    {
                        // Mevcut kategoriyle eşleşti
                        categoryId = bestMatch.CategoryId;
                        matchType = bestMatch.MatchType;
                        score = bestMatch.Score;
                        categoryName = bestMatch.CategoryName;

                        await CreateMappingAsync(group.AnagrupKod, null, categoryId,
                            $"Bulk auto-mapped ({matchType}, score={score})", cancellationToken);

                        result.NewMappingsCreated++;
                    }
                    else if (_autoCreateCategories)
                    {
                        // ADIM 10: Hiyerarşi desteği — yeni kategori oluştururken ParentId ata
                        categoryId = await CreateHierarchicalCategoryAsync(
                            group.AnagrupKod, null, cancellationToken);

                        var newCat = await _context.Categories.FindAsync(
                            new object[] { categoryId }, cancellationToken);
                        categoryName = newCat?.Name ?? group.AnagrupKod;
                        matchType = "new_category";
                        score = 0;

                        result.NewCategoriesCreated++;
                        result.NewMappingsCreated++;
                    }
                    else
                    {
                        // "Diğer"e at
                        categoryId = await GetDigerCategoryIdAsync(cancellationToken);
                        categoryName = "Diğer";
                        matchType = "fallback_diger";
                        score = 0;

                        await CreateMappingAsync(group.AnagrupKod, null, categoryId,
                            "Bulk auto-mapped → Diğer", cancellationToken);

                        result.FallbackToDiger++;
                        result.NewMappingsCreated++;
                    }

                    result.Mappings.Add(new AutoMappingEntry
                    {
                        AnagrupKod = group.AnagrupKod,
                        CategoryId = categoryId,
                        CategoryName = categoryName,
                        MatchType = matchType,
                        Score = score,
                        ProductCount = group.ProductCount
                    });
                }
                catch (Exception ex)
                {
                    result.Errors++;
                    result.ErrorDetails.Add($"{group.AnagrupKod}: {ex.Message}");
                    _logger.LogError(ex, "[AutoMapping] '{AnagrupKod}' eşlenirken hata", group.AnagrupKod);
                }
            }

            // ADIM 10: Alt grup (AltgrupKod) seviyesinde hiyerarşik mapping oluştur
            // NEDEN: Ana grup eşlemeleri yukarıda yapıldı, şimdi alt kategori hiyerarşisini kur.
            // AnagrupKod+AltgrupKod combo'suna özel mapping yoksa alt kategori oluştur.
            foreach (var subGroup in subGroupCodes)
            {
                // Combo mapping zaten var mı?
                var comboKey = $"{subGroup.AnagrupKod}|{subGroup.AltgrupKod}".ToUpperInvariant();
                if (mappedSet.Contains(comboKey))
                    continue;

                // AnagrupKod+AltgrupKod combo mapping var mı kontrol et
                var existingCombo = await _context.Set<MikroCategoryMapping>()
                    .AsNoTracking()
                    .AnyAsync(m => m.MikroAnagrupKod == subGroup.AnagrupKod
                        && m.MikroAltgrupKod == subGroup.AltgrupKod, cancellationToken);

                if (existingCombo)
                    continue;

                try
                {
                    if (_autoCreateCategories)
                    {
                        var childCategoryId = await CreateHierarchicalCategoryAsync(
                            subGroup.AnagrupKod, subGroup.AltgrupKod, cancellationToken);

                        var childCat = await _context.Categories.FindAsync(
                            new object[] { childCategoryId }, cancellationToken);

                        result.NewMappingsCreated++;
                        result.Mappings.Add(new AutoMappingEntry
                        {
                            AnagrupKod = subGroup.AnagrupKod,
                            AltgrupKod = subGroup.AltgrupKod,
                            CategoryId = childCategoryId,
                            CategoryName = childCat?.Name ?? subGroup.AltgrupKod,
                            MatchType = "hierarchy_subcategory",
                            Score = 1.0,
                            ProductCount = subGroup.ProductCount
                        });
                    }
                }
                catch (Exception ex)
                {
                    result.Errors++;
                    result.ErrorDetails.Add($"{subGroup.AnagrupKod}/{subGroup.AltgrupKod}: {ex.Message}");
                    _logger.LogError(ex,
                        "[AutoMapping] Alt grup eşlenirken hata: {Anagrup}/{Altgrup}",
                        subGroup.AnagrupKod, subGroup.AltgrupKod);
                }
            }

            _logger.LogInformation(
                "[AutoMapping] Toplu keşif tamamlandı. Toplam: {Total}, Zaten eşli: {Existing}, " +
                "Yeni mapping: {New}, Yeni kategori: {NewCat}, Diğer: {Diger}, Hata: {Err}",
                result.TotalGroupCodes, result.AlreadyMapped,
                result.NewMappingsCreated, result.NewCategoriesCreated,
                result.FallbackToDiger, result.Errors);

            return result;
        }

        // ==================== Private Helpers ====================

        /// <summary>
        /// Türkçe karakterleri ASCII'ye normalize eder ve küçük harfe çevirir.
        /// NEDEN: "MEYVE-SEBZE" vs "Meyve ve Sebze" karşılaştırması için temel adım.
        /// </summary>
        private static string NormalizeTurkish(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            var chars = new char[text.Length];
            for (int i = 0; i < text.Length; i++)
            {
                var c = text[i];
                chars[i] = TurkishCharMap.TryGetValue(c, out var mapped)
                    ? mapped
                    : char.ToLowerInvariant(c);
            }

            return new string(chars);
        }

        /// <summary>
        /// Metni kelimelere ayırır, stop word'leri çıkarır.
        /// NEDEN: "Et ve Et Ürünleri" → {"et"}, "ET URUNLERI" → {"et"} → exact match!
        /// </summary>
        private static HashSet<string> Tokenize(string normalizedText)
        {
            var tokens = normalizedText
                .Split(new[] { ' ', '-', '_', '/', '&', ',', '.' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(t => t.Length > 1) // Tek karakterleri atla
                .Where(t => !StopWords.Contains(t))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            return tokens;
        }

        /// <summary>
        /// Jaccard benzerlik katsayısı: |A ∩ B| / |A ∪ B|
        /// NEDEN: Kelime bazlı çalıştığı için "ET URUNLERI" ↔ "Et ve Et Ürünleri"
        /// gibi token sırası farklı olan metinlerde doğru sonuç verir.
        /// </summary>
        private static double CalculateJaccardSimilarity(HashSet<string> tokensA, HashSet<string> tokensB)
        {
            if (tokensA.Count == 0 || tokensB.Count == 0)
                return 0;

            var intersection = tokensA.Count(t => tokensB.Contains(t));
            var union = tokensA.Union(tokensB, StringComparer.OrdinalIgnoreCase).Count();

            return union > 0 ? (double)intersection / union : 0;
        }

        /// <summary>
        /// ADIM 10: Hiyerarşik kategori oluşturma.
        /// 
        /// NEDEN: E-ticaret filtreleme ve navigasyonunda hiyerarşi kritik.
        /// AnagrupKod → Ana kategori (ParentId=null), AltgrupKod → Alt kategori (ParentId=ana kategori).
        /// Örnek: AnagrupKod="GIDA", AltgrupKod="SUTURUN" → Gıda > Süt Ürünleri
        /// 
        /// AltgrupKod null ise sadece ana kategori oluşturulur.
        /// Kategori/slug mevcutsa idempotent davranır.
        /// </summary>
        private async Task<int> CreateHierarchicalCategoryAsync(
            string anagrupKod, string? altgrupKod, CancellationToken cancellationToken)
        {
            // 1. Ana kategori oluştur/bul (ParentId=null)
            var parentCategoryId = await EnsureCategoryAsync(
                anagrupKod, parentId: null, cancellationToken);

            // AltgrupKod yoksa ana kategori ID'si yeterli
            if (string.IsNullOrWhiteSpace(altgrupKod))
            {
                await CreateMappingAsync(anagrupKod, null, parentCategoryId,
                    "Ana kategori otomatik oluşturuldu (hiyerarşi)", cancellationToken);
                return parentCategoryId;
            }

            // 2. Alt kategori oluştur/bul (ParentId=ana kategori)
            var childCategoryId = await EnsureCategoryAsync(
                altgrupKod, parentId: parentCategoryId, cancellationToken);

            await CreateMappingAsync(anagrupKod, altgrupKod, childCategoryId,
                $"Alt kategori otomatik oluşturuldu (ParentId={parentCategoryId})", cancellationToken);

            _logger.LogInformation(
                "[AutoMapping] Hiyerarşik kategori: {Anagrup}(Id={ParentId}) > {Altgrup}(Id={ChildId})",
                anagrupKod, parentCategoryId, altgrupKod, childCategoryId);

            return childCategoryId;
        }

        /// <summary>
        /// Kategori varsa döner, yoksa oluşturur. Hiyerarşi yapısını korur.
        /// </summary>
        private async Task<int> EnsureCategoryAsync(
            string groupCode, int? parentId, CancellationToken cancellationToken)
        {
            var prettyName = PrettifyGroupCode(groupCode);
            var slug = GenerateSlug(prettyName);

            // ParentId ile birlikte slug bazlı kontrol — aynı isimde farklı seviyede kategori olabilir
            var existing = await _context.Categories
                .FirstOrDefaultAsync(c => c.Slug == slug && c.ParentId == parentId, cancellationToken);

            if (existing != null)
                return existing.Id;

            // Slug çakışması kontrolü — farklı parent'ta aynı slug varsa suffix ekle
            var slugExists = await _context.Categories
                .AnyAsync(c => c.Slug == slug, cancellationToken);

            if (slugExists && parentId.HasValue)
            {
                // Parent slug'ını prefix olarak ekle: "gida-suturun" gibi
                var parentCat = await _context.Categories.FindAsync(
                    new object[] { parentId.Value }, cancellationToken);
                if (parentCat != null)
                    slug = $"{parentCat.Slug}-{slug}";
            }

            var category = new Category
            {
                Name = prettyName,
                Description = parentId.HasValue
                    ? $"Mikro ERP '{groupCode}' alt grup kodundan otomatik oluşturuldu"
                    : $"Mikro ERP '{groupCode}' ana grup kodundan otomatik oluşturuldu",
                Slug = slug,
                ParentId = parentId,
                SortOrder = parentId.HasValue ? 10 : 100,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync(cancellationToken);

            return category.Id;
        }

        /// <summary>
        /// MikroCategoryMapping kaydı oluşturur (AddMappingAsync upsert yapıyor).
        /// </summary>
        private async Task CreateMappingAsync(
            string anagrupKod, string? altgrupKod, int categoryId,
            string notes, CancellationToken cancellationToken)
        {
            await _mappingService.AddMappingAsync(new MikroCategoryMapping
            {
                MikroAnagrupKod = anagrupKod,
                MikroAltgrupKod = altgrupKod,
                CategoryId = categoryId,
                Priority = 10, // Otomatik eşlemeler orta öncelik
                Notes = notes
            }, cancellationToken);
        }

        /// <summary>
        /// "Diğer" kategorisinin Id'sini döner.
        /// NEDEN: CategorySeeder tarafından garanti ediliyor, ama yine de fallback koruması var.
        /// </summary>
        private async Task<int> GetDigerCategoryIdAsync(CancellationToken cancellationToken)
        {
            var diger = await _context.Categories
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Slug == "diger", cancellationToken);

            if (diger != null)
                return diger.Id;

            // CategorySeeder çalışmamış — log yaz ve ilk kategoriye fallback
            _logger.LogError("[AutoMapping] 'Diğer' kategorisi bulunamadı! CategorySeeder kontrol edin.");
            return 1;
        }

        /// <summary>
        /// Mikro grup kodunu insana okunur kategoriye dönüştürür.
        /// "MEYVE-SEBZE" → "Meyve Sebze", "ET_URUNLERI" → "Et Ürünleri"
        /// </summary>
        private static string PrettifyGroupCode(string groupCode)
        {
            if (string.IsNullOrWhiteSpace(groupCode))
                return "Bilinmeyen Kategori";

            // Tire/alt çizgiyi boşluğa çevir
            var pretty = groupCode.Replace('-', ' ').Replace('_', ' ');

            // Her kelimenin ilk harfini büyük yap (Title Case)
            var words = pretty.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < words.Length; i++)
            {
                if (words[i].Length > 0)
                {
                    words[i] = char.ToUpper(words[i][0]) +
                        (words[i].Length > 1 ? words[i][1..].ToLowerInvariant() : "");
                }
            }

            return string.Join(' ', words);
        }

        /// <summary>
        /// URL-dostu slug oluşturur (Türkçe karakter desteğiyle).
        /// </summary>
        private static string GenerateSlug(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return string.Empty;

            var slug = NormalizeTurkish(name.Trim());

            // Alfanümerik olmayan karakterleri tire ile değiştir
            var chars = new char[slug.Length];
            for (int i = 0; i < slug.Length; i++)
            {
                chars[i] = char.IsLetterOrDigit(slug[i]) ? slug[i] : '-';
            }

            slug = new string(chars);

            // Ardışık tireleri tek tireye indir
            while (slug.Contains("--"))
                slug = slug.Replace("--", "-");

            return slug.Trim('-');
        }
    }
}
