using System.Diagnostics;
using ECommerce.Core.Interfaces.Mapping;
using ECommerce.Core.Interfaces.Sync;
using ECommerce.Data.Context;
using ECommerce.Entities.Concrete;
using ECommerce.Entities.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ECommerce.Business.Services.Sync
{
    /// <summary>
    /// Mikro → E-Ticaret ürün bilgi senkronizasyonu (cache → Product tablosu).
    /// 
    /// NEDEN: HotPoll yalnızca stok/fiyat değişikliklerini Product tablosuna yansıtıyor.
    /// İsim, açıklama, kategori, birim, KDV, barkod, ağırlık ve aktif/pasif durumu
    /// bu servisle senkronize edilir.
    /// 
    /// TASARIM PRENSİBİ:
    /// - Mevcut Product verisi korunur, sadece Mikro'da değişen alanlar güncellenir
    /// - Kategori eşleme MikroCategoryMapping tablosu üzerinden yapılır
    /// - Birim eşleme MikroStokMapper'daki mantığı izler (DRY)
    /// - Partial failure tolere edilir — hatalı ürünler atlanır, loglanır
    /// </summary>
    public class ProductInfoSyncService : IProductInfoSyncService
    {
        private readonly ECommerceDbContext _context;
        private readonly ILogger<ProductInfoSyncService> _logger;
        // NEDEN: Mapping tablosunda eşleme bulunamazsa otomatik oluşturur (lazy auto-map)
        private readonly IAutoCategoryMappingEngine? _autoMappingEngine;

        // Birim → WeightUnit eşleme tablosu (MikroStokMapper ile tutarlı)
        private static readonly Dictionary<string, WeightUnit> BirimMappings = new(StringComparer.OrdinalIgnoreCase)
        {
            ["ADET"] = WeightUnit.Piece, ["AD"] = WeightUnit.Piece,
            ["KG"] = WeightUnit.Kilogram, ["KILOGRAM"] = WeightUnit.Kilogram, ["KİLOGRAM"] = WeightUnit.Kilogram,
            ["GR"] = WeightUnit.Gram, ["GRAM"] = WeightUnit.Gram,
            ["LT"] = WeightUnit.Liter, ["LİTRE"] = WeightUnit.Liter, ["LITRE"] = WeightUnit.Liter,
            ["ML"] = WeightUnit.Milliliter, ["MİLİLİTRE"] = WeightUnit.Milliliter,
            ["PAKET"] = WeightUnit.Piece, ["KUTU"] = WeightUnit.Piece,
            ["ŞİŞE"] = WeightUnit.Piece, ["SISE"] = WeightUnit.Piece,
            ["DEMET"] = WeightUnit.Piece
        };

        // Ağırlık bazlı birimler — bu birimlerdeki ürünler IsWeightBased=true olur
        private static readonly HashSet<WeightUnit> WeightBasedUnits = new()
        {
            WeightUnit.Kilogram, WeightUnit.Gram, WeightUnit.Liter, WeightUnit.Milliliter
        };

        public ProductInfoSyncService(
            ECommerceDbContext context,
            ILogger<ProductInfoSyncService> logger,
            IAutoCategoryMappingEngine? autoMappingEngine = null)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _autoMappingEngine = autoMappingEngine;
        }

        /// <inheritdoc />
        public async Task<ProductInfoSyncResult> SyncProductInfoFromCacheAsync(
            string stokKod,
            CancellationToken cancellationToken = default)
        {
            var sw = Stopwatch.StartNew();

            var cache = await _context.MikroProductCaches
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.StokKod == stokKod, cancellationToken);

            if (cache == null)
                return ProductInfoSyncResult.Fail($"Cache'de bulunamadı: {stokKod}");

            if (!cache.LocalProductId.HasValue)
                return ProductInfoSyncResult.Fail($"LocalProductId atanmamış: {stokKod}");

            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == cache.LocalProductId.Value, cancellationToken);

            if (product == null)
                return ProductInfoSyncResult.Fail($"Product bulunamadı: ID={cache.LocalProductId}");

            var result = new ProductInfoSyncResult { Success = true, TotalProcessed = 1 };
            ApplyInfoChanges(cache, product, result);

            await _context.SaveChangesAsync(cancellationToken);

            sw.Stop();
            result.DurationMs = sw.ElapsedMilliseconds;

            _logger.LogDebug(
                "[ProductInfoSync] Tek ürün senkronize edildi. SKU: {Sku}, Süre: {Ms}ms",
                stokKod, sw.ElapsedMilliseconds);

            return result;
        }

        /// <inheritdoc />
        public async Task<ProductInfoSyncResult> SyncBatchProductInfoAsync(
            IEnumerable<string> stokKodlar,
            CancellationToken cancellationToken = default)
        {
            var sw = Stopwatch.StartNew();
            var skuList = stokKodlar.ToList();

            if (skuList.Count == 0)
                return ProductInfoSyncResult.Ok(0, 0);

            // Cache kayıtlarını çek
            var cacheRecords = await _context.MikroProductCaches
                .AsNoTracking()
                .Where(c => skuList.Contains(c.StokKod) && c.LocalProductId.HasValue)
                .ToListAsync(cancellationToken);

            if (cacheRecords.Count == 0)
                return ProductInfoSyncResult.Ok(0, 0);

            // İlişkili ürünleri çek
            var productIds = cacheRecords
                .Select(c => c.LocalProductId!.Value)
                .ToList();

            var products = await _context.Products
                .Where(p => productIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, cancellationToken);

            // Kategori eşlemelerini çek (batch için tek sorgu)
            var grupKodlar = cacheRecords.Select(c => c.GrupKod).Where(g => g != null).Distinct().ToList();
            var categoryMappings = await LoadCategoryMappingsAsync(grupKodlar!, cancellationToken);

            var result = new ProductInfoSyncResult { Success = true };

            foreach (var cache in cacheRecords)
            {
                if (!products.TryGetValue(cache.LocalProductId!.Value, out var product))
                {
                    result.Skipped++;
                    continue;
                }

                try
                {
                    ApplyInfoChanges(cache, product, result, categoryMappings);
                    result.TotalProcessed++;
                }
                catch (Exception ex)
                {
                    result.Errors++;
                    result.ErrorDetails.Add($"{cache.StokKod}: {ex.Message}");
                    _logger.LogWarning(ex,
                        "[ProductInfoSync] Batch sync hatası. SKU: {Sku}", cache.StokKod);
                }
            }

            await _context.SaveChangesAsync(cancellationToken);

            sw.Stop();
            result.DurationMs = sw.ElapsedMilliseconds;

            _logger.LogInformation(
                "[ProductInfoSync] Batch senkronizasyon tamamlandı. " +
                "İşlenen: {Processed}, İsim: {Names}, Kategori: {Categories}, Ağırlık: {Weight}, Hata: {Errors}",
                result.TotalProcessed, result.NamesUpdated, result.CategoriesUpdated,
                result.WeightInfoUpdated, result.Errors);

            return result;
        }

        /// <inheritdoc />
        public async Task<ProductInfoSyncResult> SyncAllProductInfoAsync(
            CancellationToken cancellationToken = default)
        {
            var sw = Stopwatch.StartNew();

            _logger.LogInformation("[ProductInfoSync] Tam bilgi senkronizasyonu başlıyor...");

            // Tüm eşlenmiş cache kayıtlarını çek
            var cacheRecords = await _context.MikroProductCaches
                .AsNoTracking()
                .Where(c => c.LocalProductId.HasValue && c.Aktif)
                .ToListAsync(cancellationToken);

            var productIds = cacheRecords
                .Select(c => c.LocalProductId!.Value)
                .ToList();

            var products = await _context.Products
                .Where(p => productIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, cancellationToken);

            // Tüm kategori eşlemelerini çek
            var categoryMappings = await _context.Set<MikroCategoryMapping>()
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            var mappingLookup = BuildCategoryMappingLookup(categoryMappings);
            var result = new ProductInfoSyncResult { Success = true };

            foreach (var cache in cacheRecords)
            {
                if (!products.TryGetValue(cache.LocalProductId!.Value, out var product))
                {
                    result.Skipped++;
                    continue;
                }

                try
                {
                    ApplyInfoChanges(cache, product, result, mappingLookup);
                    result.TotalProcessed++;
                }
                catch (Exception ex)
                {
                    result.Errors++;
                    result.ErrorDetails.Add($"{cache.StokKod}: {ex.Message}");
                }
            }

            await _context.SaveChangesAsync(cancellationToken);

            sw.Stop();
            result.DurationMs = sw.ElapsedMilliseconds;

            _logger.LogInformation(
                "[ProductInfoSync] Tam senkronizasyon tamamlandı. " +
                "Toplam: {Total}, İsim: {Names}, Kategori: {Cat}, Ağırlık: {Weight}, Durum: {Status}, Hata: {Err}",
                result.TotalProcessed, result.NamesUpdated, result.CategoriesUpdated,
                result.WeightInfoUpdated, result.StatusUpdated, result.Errors);

            return result;
        }

        /// <inheritdoc />
        public async Task<int> SyncProductCategoriesAsync(
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("[ProductInfoSync] Kategori senkronizasyonu başlıyor...");

            var mappings = await _context.Set<MikroCategoryMapping>()
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            if (mappings.Count == 0)
            {
                _logger.LogWarning("[ProductInfoSync] Hiç kategori eşlemesi tanımlı değil.");
                return 0;
            }

            var mappingLookup = BuildCategoryMappingLookup(mappings);
            int updated = 0;

            // AnagrupKod veya GrupKod dolu olan cache kayıtlarını çek
            // NEDEN: AnagrupKod asıl eşleme anahtarı, GrupKod (altgrup) spesifik eşleme için
            var cacheWithGroup = await _context.MikroProductCaches
                .AsNoTracking()
                .Where(c => c.LocalProductId.HasValue &&
                    (!string.IsNullOrEmpty(c.AnagrupKod) || !string.IsNullOrEmpty(c.GrupKod)))
                .Select(c => new { c.LocalProductId, c.GrupKod, c.AnagrupKod })
                .ToListAsync(cancellationToken);

            var productIds = cacheWithGroup
                .Select(c => c.LocalProductId!.Value)
                .Distinct()
                .ToList();

            var products = await _context.Products
                .Where(p => productIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, cancellationToken);

            foreach (var cacheItem in cacheWithGroup)
            {
                if (!products.TryGetValue(cacheItem.LocalProductId!.Value, out var product))
                    continue;

                // Önce AnagrupKod ile ara (doğru semantik), yoksa GrupKod fallback
                var lookupKey = !string.IsNullOrEmpty(cacheItem.AnagrupKod)
                    ? cacheItem.AnagrupKod
                    : cacheItem.GrupKod!;

                var mapping = FindBestCategoryMapping(lookupKey, mappingLookup);

                // Wildcard fallback — hiçbir eşleme bulunamazsa "*" dene
                mapping ??= FindBestCategoryMapping("*", mappingLookup);

                // ADIM 5: Hâlâ mapping yoksa AutoCategoryMappingEngine ile otomatik oluştur
                // NEDEN: Yeni ürün grupları geldiğinde mapping tablosu boş olabilir.
                // Engine fuzzy match ile mevcut kategoriye eşler veya yeni kategori oluşturur.
                if (mapping == null && _autoMappingEngine != null &&
                    !string.IsNullOrEmpty(cacheItem.AnagrupKod))
                {
                    try
                    {
                        var resolvedCategoryId = await _autoMappingEngine.ResolveOrCreateMappingAsync(
                            cacheItem.AnagrupKod, cacheItem.GrupKod, cancellationToken);

                        if (product.CategoryId != resolvedCategoryId)
                        {
                            product.CategoryId = resolvedCategoryId;
                            updated++;
                        }

                        continue; // Mapping engine zaten DB'ye yazdı, sonraki ürüne geç
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex,
                            "[ProductInfoSync] AutoMapping hatası: {AnagrupKod}", cacheItem.AnagrupKod);
                    }
                }

                if (mapping != null && product.CategoryId != mapping.CategoryId)
                {
                    product.CategoryId = mapping.CategoryId;
                    if (mapping.BrandId.HasValue)
                        product.BrandId = mapping.BrandId;
                    updated++;
                }
            }

            if (updated > 0)
                await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "[ProductInfoSync] Kategori senkronizasyonu tamamlandı. Güncellenen: {Updated}", updated);

            return updated;
        }

        /// <inheritdoc />
        public async Task<List<UnmappedGroupInfo>> GetUnmappedGroupCodesAsync(
            CancellationToken cancellationToken = default)
        {
            // NEDEN: AnagrupKod asıl eşleme anahtarı, GrupKod (altgrup) ek filtre.
            // Her iki alanı da keşfetmemiz lazım ki mapping tablo tam oluşturulsun.
            var allGroupCodes = await _context.MikroProductCaches
                .AsNoTracking()
                .Where(c => !string.IsNullOrEmpty(c.AnagrupKod) || !string.IsNullOrEmpty(c.GrupKod))
                .GroupBy(c => new { c.AnagrupKod, c.GrupKod })
                .Select(g => new
                {
                    AnagrupKod = g.Key.AnagrupKod,
                    GrupKod = g.Key.GrupKod,
                    ProductCount = g.Count(),
                    SampleStokKod = g.Min(c => c.StokKod),
                    SampleStokAd = g.Min(c => c.StokAd)
                })
                .ToListAsync(cancellationToken);

            // Eşlenmiş anagrup kodlarını çek
            var mappedCodes = await _context.Set<MikroCategoryMapping>()
                .AsNoTracking()
                .Select(m => m.MikroAnagrupKod)
                .Distinct()
                .ToListAsync(cancellationToken);

            var mappedSet = new HashSet<string>(mappedCodes, StringComparer.OrdinalIgnoreCase);

            return allGroupCodes
                .Where(g => !string.IsNullOrEmpty(g.AnagrupKod) && !mappedSet.Contains(g.AnagrupKod!))
                .Select(g => new UnmappedGroupInfo
                {
                    GrupKod = g.AnagrupKod!,
                    AltgrupKod = g.GrupKod,
                    ProductCount = g.ProductCount,
                    SampleStokKod = g.SampleStokKod,
                    SampleStokAd = g.SampleStokAd
                })
                .OrderByDescending(g => g.ProductCount)
                .ToList();
        }

        /// <inheritdoc />
        public async Task<ImageSyncStatusReport> GetImageSyncStatusAsync(
            CancellationToken cancellationToken = default)
        {
            var totalProducts = await _context.Products.CountAsync(cancellationToken);

            // Resmi olan ürünler — ProductImage tablosunda veya ImageUrl dolu
            var productsWithImages = await _context.Products
                .CountAsync(p =>
                    !string.IsNullOrEmpty(p.ImageUrl) ||
                    p.ProductImages.Any(),
                    cancellationToken);

            var withoutImages = totalProducts - productsWithImages;

            // İlk 50 resmi eksik ürünü listele
            var missingImageProducts = await _context.Products
                .Where(p =>
                    string.IsNullOrEmpty(p.ImageUrl) &&
                    !p.ProductImages.Any())
                .OrderBy(p => p.Name)
                .Take(50)
                .Select(p => new MissingImageProduct
                {
                    ProductId = p.Id,
                    SKU = p.SKU,
                    Name = p.Name
                })
                .ToListAsync(cancellationToken);

            return new ImageSyncStatusReport
            {
                TotalProducts = totalProducts,
                ProductsWithImages = productsWithImages,
                ProductsWithoutImages = withoutImages,
                CoveragePercent = totalProducts > 0
                    ? Math.Round((decimal)productsWithImages / totalProducts * 100, 1)
                    : 0,
                MissingImageProducts = missingImageProducts
            };
        }

        // ==================== Private Helpers ====================

        /// <summary>
        /// Cache'den Product'a bilgi alanlarını uygular.
        /// Sadece değişen alanlar güncellenir — gereksiz write yok.
        /// </summary>
        private void ApplyInfoChanges(
            MikroProductCache cache,
            Product product,
            ProductInfoSyncResult result,
            Dictionary<string, List<MikroCategoryMapping>>? categoryMappings = null)
        {
            bool changed = false;

            // İsim güncelleme — boş olmadığında ve farklı olduğunda
            if (!string.IsNullOrWhiteSpace(cache.StokAd) &&
                !string.Equals(product.Name, cache.StokAd.Trim(), StringComparison.Ordinal))
            {
                product.Name = cache.StokAd.Trim();
                product.Slug = GenerateSlug(cache.StokAd);
                result.NamesUpdated++;
                changed = true;
            }

            // Birim / ağırlık bazlı satış güncelleme
            if (!string.IsNullOrWhiteSpace(cache.Birim))
            {
                var newUnit = MapBirimToWeightUnit(cache.Birim);
                if (product.WeightUnit != newUnit)
                {
                    product.WeightUnit = newUnit;
                    product.IsWeightBased = WeightBasedUnits.Contains(newUnit);
                    result.WeightInfoUpdated++;
                    changed = true;
                }
            }

            // Kategori güncelleme — AnagrupKod öncelikli, GrupKod fallback
            if (categoryMappings != null &&
                (!string.IsNullOrWhiteSpace(cache.AnagrupKod) || !string.IsNullOrWhiteSpace(cache.GrupKod)))
            {
                var lookupKey = !string.IsNullOrEmpty(cache.AnagrupKod)
                    ? cache.AnagrupKod
                    : cache.GrupKod!;

                var mapping = FindBestCategoryMapping(lookupKey, categoryMappings)
                    ?? FindBestCategoryMapping("*", categoryMappings); // wildcard fallback
                if (mapping != null && product.CategoryId != mapping.CategoryId)
                {
                    product.CategoryId = mapping.CategoryId;
                    if (mapping.BrandId.HasValue)
                        product.BrandId = mapping.BrandId;
                    result.CategoriesUpdated++;
                    changed = true;
                }
            }

            // Aktif/pasif durum — cache'deki Aktif alanı ile Product.IsActive karşılaştır
            if (product.IsActive != cache.Aktif)
            {
                product.IsActive = cache.Aktif;
                result.StatusUpdated++;
                changed = true;
            }

            if (changed)
            {
                product.UpdatedAt = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// Kategori eşleme tablosunu GrupKod bazlı lookup dict'e dönüştürür.
        /// </summary>
        private async Task<Dictionary<string, List<MikroCategoryMapping>>> LoadCategoryMappingsAsync(
            List<string> grupKodlar,
            CancellationToken cancellationToken)
        {
            var mappings = await _context.Set<MikroCategoryMapping>()
                .AsNoTracking()
                .Where(m => grupKodlar.Contains(m.MikroAnagrupKod))
                .ToListAsync(cancellationToken);

            return BuildCategoryMappingLookup(mappings);
        }

        private static Dictionary<string, List<MikroCategoryMapping>> BuildCategoryMappingLookup(
            List<MikroCategoryMapping> mappings)
        {
            return mappings
                .GroupBy(m => m.MikroAnagrupKod, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderByDescending(m => m.Priority).ToList(),
                    StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// GrupKod için en iyi eşlemeyi bulur (Priority DESC).
        /// </summary>
        private static MikroCategoryMapping? FindBestCategoryMapping(
            string grupKod,
            Dictionary<string, List<MikroCategoryMapping>> lookup)
        {
            if (lookup.TryGetValue(grupKod, out var mappings) && mappings.Count > 0)
                return mappings[0]; // En yüksek priority'li eşleme

            return null;
        }

        private static WeightUnit MapBirimToWeightUnit(string birim)
        {
            return BirimMappings.TryGetValue(birim.Trim(), out var unit)
                ? unit
                : WeightUnit.Piece;
        }

        /// <summary>
        /// URL-dostu slug oluşturur. MikroStokMapper ile tutarlı mantık.
        /// </summary>
        private static string GenerateSlug(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return string.Empty;

            var slug = name.Trim().ToLowerInvariant();

            // Türkçe karakter dönüşümü
            slug = slug
                .Replace("ı", "i").Replace("ğ", "g").Replace("ü", "u")
                .Replace("ş", "s").Replace("ö", "o").Replace("ç", "c")
                .Replace("İ", "i").Replace("Ğ", "g").Replace("Ü", "u")
                .Replace("Ş", "s").Replace("Ö", "o").Replace("Ç", "c");

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

        /// <inheritdoc />
        public async Task<RecategorizeResult> RecategorizeAllProductsAsync(
            CancellationToken cancellationToken = default)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var result = new RecategorizeResult();

            _logger.LogInformation("[ProductInfoSync] ADIM 7: Toplu yeniden kategorileme başlıyor...");

            // AŞAMA 1: Auto-Mapping Engine ile tüm grup kodlarını keşfet + eşle
            // NEDEN: Önce mapping tablosunu doldurmalıyız ki SyncProductCategoriesAsync 
            // doğru çalışsın. Engine yeni kategoriler oluşturur veya mevcut kategorilere eşler.
            if (_autoMappingEngine != null)
            {
                try
                {
                    var autoResult = await _autoMappingEngine.DiscoverAndMapAllAsync(cancellationToken);
                    result.NewMappingsCreated = autoResult.NewMappingsCreated;
                    result.NewCategoriesCreated = autoResult.NewCategoriesCreated;
                    result.FallbackToDiger = autoResult.FallbackToDiger;

                    _logger.LogInformation(
                        "[ProductInfoSync] ADIM 7 Aşama 1 tamamlandı. Yeni mapping: {New}, Yeni kategori: {NewCat}, Diğer: {Diger}",
                        autoResult.NewMappingsCreated, autoResult.NewCategoriesCreated, autoResult.FallbackToDiger);

                    if (autoResult.Errors > 0)
                    {
                        result.ErrorDetails.AddRange(autoResult.ErrorDetails);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[ProductInfoSync] ADIM 7 Aşama 1: Auto-mapping motoru hatası");
                    result.ErrorDetails.Add($"Auto-mapping hatası: {ex.Message}");
                }
            }
            else
            {
                _logger.LogWarning("[ProductInfoSync] ADIM 7: IAutoCategoryMappingEngine inject edilmemiş, " +
                    "sadece mevcut mapping tablosuyla çalışılacak.");
            }

            // AŞAMA 2: Tüm ürünlerin CategoryId'sini güncelle (mapping tablosuna göre)
            // NEDEN: Aşama 1'de mapping tablosu dolduruldu, şimdi Product.CategoryId değerlerini
            // bu mapping'lere göre güncelliyoruz.
            try
            {
                result.CategoriesUpdated = await SyncProductCategoriesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ProductInfoSync] ADIM 7 Aşama 2: Kategori sync hatası");
                result.ErrorDetails.Add($"Kategori sync hatası: {ex.Message}");
                result.Errors++;
            }

            // AŞAMA 3: Cache'e bağlı olmayan ürünleri "Diğer"e at
            // NEDEN: Bazı ürünler MikroProductCache'te olmayabilir (eski import, manuel ekleme).
            // Bu ürünlerin de geçerli bir kategorisi olmalı.
            try
            {
                var digerCategory = await _context.Categories
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Slug == "diger", cancellationToken);

                if (digerCategory != null)
                {
                    // Category FK'sı geçersiz olan ürünleri bul (silinmiş kategori, 0, vs.)
                    var validCategoryIds = await _context.Categories
                        .AsNoTracking()
                        .Select(c => c.Id)
                        .ToListAsync(cancellationToken);

                    var validCategorySet = new HashSet<int>(validCategoryIds);

                    var orphanProducts = await _context.Products
                        .Where(p => !validCategorySet.Contains(p.CategoryId))
                        .ToListAsync(cancellationToken);

                    if (orphanProducts.Count > 0)
                    {
                        foreach (var product in orphanProducts)
                        {
                            product.CategoryId = digerCategory.Id;
                        }
                        await _context.SaveChangesAsync(cancellationToken);

                        _logger.LogInformation(
                            "[ProductInfoSync] ADIM 7 Aşama 3: {Count} yetim ürün 'Diğer' kategorisine taşındı",
                            orphanProducts.Count);

                        result.CategoriesUpdated += orphanProducts.Count;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ProductInfoSync] ADIM 7 Aşama 3: Yetim ürün düzeltme hatası");
                result.ErrorDetails.Add($"Yetim ürün hatası: {ex.Message}");
                result.Errors++;
            }

            sw.Stop();
            result.TotalProducts = await _context.Products.CountAsync(cancellationToken);
            result.DurationMs = sw.ElapsedMilliseconds;
            result.Success = result.Errors == 0;
            result.Message = $"Toplu kategorileme tamamlandı. " +
                $"Toplam ürün: {result.TotalProducts}, Kategori güncellenen: {result.CategoriesUpdated}, " +
                $"Yeni mapping: {result.NewMappingsCreated}, Yeni kategori: {result.NewCategoriesCreated}, " +
                $"Diğer'e atanan: {result.FallbackToDiger}, Süre: {result.DurationMs}ms";

            _logger.LogInformation("[ProductInfoSync] ADIM 7: {Message}", result.Message);

            return result;
        }

        /// <inheritdoc />
        public async Task<int> MoveProductsToDigerAsync(
            int categoryId,
            CancellationToken cancellationToken = default)
        {
            // ADIM 11: Kategori silindiğinde/deaktif edildiğinde ürünleri "Diğer"e taşı
            var digerCategory = await _context.Categories
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Slug == "diger", cancellationToken);

            if (digerCategory == null)
            {
                _logger.LogError("[ProductInfoSync] 'Diğer' kategorisi bulunamadı! MoveProducts iptal.");
                return 0;
            }

            // Hedef kategori zaten "Diğer" ise işlem yok
            if (categoryId == digerCategory.Id)
                return 0;

            var affectedProducts = await _context.Products
                .Where(p => p.CategoryId == categoryId)
                .ToListAsync(cancellationToken);

            if (affectedProducts.Count == 0)
                return 0;

            foreach (var product in affectedProducts)
            {
                product.CategoryId = digerCategory.Id;
                product.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "[ProductInfoSync] ADIM 11: {Count} ürün CategoryId={OldCat}'den 'Diğer'e taşındı",
                affectedProducts.Count, categoryId);

            return affectedProducts.Count;
        }

        /// <inheritdoc />
        public async Task<int> ResyncProductsByAnagrupKodAsync(
            string anagrupKod,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(anagrupKod))
                return 0;

            // ADIM 11: Mapping değişikliğinden etkilenen ürünleri yeniden kategorile
            // NEDEN: Admin bir mapping'i güncellediğinde, o AnagrupKod'lu ürünler 
            // yeni CategoryId'ye taşınmalı. Tüm ürünleri taramak yerine sadece etkilenenleri tara.
            var affectedCacheItems = await _context.MikroProductCaches
                .AsNoTracking()
                .Where(c => c.AnagrupKod == anagrupKod && c.LocalProductId.HasValue)
                .Select(c => new { c.LocalProductId, c.GrupKod, c.AnagrupKod })
                .ToListAsync(cancellationToken);

            if (affectedCacheItems.Count == 0)
                return 0;

            var productIds = affectedCacheItems.Select(c => c.LocalProductId!.Value).Distinct().ToList();
            var products = await _context.Products
                .Where(p => productIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, cancellationToken);

            // Güncel mapping'i çek
            var mappings = await _context.Set<MikroCategoryMapping>()
                .AsNoTracking()
                .Where(m => m.MikroAnagrupKod == anagrupKod || m.MikroAnagrupKod == "*")
                .ToListAsync(cancellationToken);

            var mappingLookup = BuildCategoryMappingLookup(mappings);
            int updated = 0;

            foreach (var cacheItem in affectedCacheItems)
            {
                if (!products.TryGetValue(cacheItem.LocalProductId!.Value, out var product))
                    continue;

                var lookupKey = !string.IsNullOrEmpty(cacheItem.AnagrupKod)
                    ? cacheItem.AnagrupKod
                    : cacheItem.GrupKod!;

                var mapping = FindBestCategoryMapping(lookupKey, mappingLookup)
                    ?? FindBestCategoryMapping("*", mappingLookup);

                if (mapping != null && product.CategoryId != mapping.CategoryId)
                {
                    product.CategoryId = mapping.CategoryId;
                    if (mapping.BrandId.HasValue)
                        product.BrandId = mapping.BrandId;
                    product.UpdatedAt = DateTime.UtcNow;
                    updated++;
                }
            }

            if (updated > 0)
                await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "[ProductInfoSync] ADIM 11: AnagrupKod='{Anagrup}' için {Updated}/{Total} ürün güncellendi",
                anagrupKod, updated, affectedCacheItems.Count);

            return updated;
        }
    }
}
