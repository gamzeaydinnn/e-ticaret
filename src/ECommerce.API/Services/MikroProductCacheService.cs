using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ECommerce.Core.DTOs.Micro;
using ECommerce.Core.Interfaces.Cache;
using ECommerce.Core.Interfaces.Mapping;
using ECommerce.Data.Context;
using ECommerce.Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MikroApiService = ECommerce.Infrastructure.Services.MicroServices.MicroService;

namespace ECommerce.API.Services
{
    /// <summary>
    /// Mikro ürün cache servisi implementasyonu
    /// </summary>
    public class MikroProductCacheService : IMikroProductCacheService
    {
        private readonly ECommerceDbContext _context;
        private readonly MikroApiService _mikroApiService;
        private readonly ILogger<MikroProductCacheService> _logger;
        // NEDEN: Cache sync sonrası yeni gelen grup kodlarını otomatik eşle
        private readonly IAutoCategoryMappingEngine? _autoMappingEngine;
        private static readonly SemaphoreSlim CacheWarmupLock = new(1, 1);

        // Batch işlem boyutları - performans optimizasyonu
        // NOT: 500 sayfa boyutu ve 100ms throttle UI'ı kitliyordu.
        // 150 sayfa + 500ms throttle ile sunucu yükü ve UI responsiveness dengelendi.
        private const int FETCH_PAGE_SIZE = 150;      // Mikro'dan çekilecek sayfa boyutu (500'den düşürüldü)
        private const int BATCH_INSERT_SIZE = 100;    // DB'ye yazılacak batch boyutu
        private const int THROTTLE_DELAY_MS = 500;    // API istekleri arası bekleme (100ms'den artırıldı)

        public MikroProductCacheService(
            ECommerceDbContext context,
            MikroApiService mikroApiService,
            ILogger<MikroProductCacheService> logger,
            IAutoCategoryMappingEngine? autoMappingEngine = null)
        {
            _context = context;
            _mikroApiService = mikroApiService;
            _logger = logger;
            _autoMappingEngine = autoMappingEngine;
        }

        /// <summary>
        /// Cache'den sayfalı ürün listesi getirir - ÇOK HIZLI (local DB)
        /// NOT: Auto-warmup kaldırıldı. Cache boşsa kullanıcı manuel "Ürünleri Yükle" butonunu kullanmalı.
        /// Bu sayede sayfa açılışında UI bloke olmuyor.
        /// </summary>
        public async Task<MikroCachePageResult> GetCachedProductsAsync(MikroCacheQuery query)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();

            // AUTO-WARMUP KALDIRILDI: Cache boşken otomatik doldurma UI'ı bloke ediyordu.
            // Kullanıcı manuel olarak "Ürünleri Yükle" / "Toplu Çek" butonunu kullanmalı.
            var hasAnyCache = await _context.MikroProductCaches.AnyAsync();
            if (!hasAnyCache)
            {
                _logger.LogInformation(
                    "[MikroProductCacheService] Cache boş. Kullanıcıdan manuel sync bekleniyor.");
                
                // Boş sonuç dön, kullanıcı arayüzde uyarı görecek
                return new MikroCachePageResult
                {
                    Items = new List<MikroProductCache>(),
                    TotalCount = 0,
                    Page = query.Page,
                    PageSize = query.PageSize,
                    LastSyncTime = null
                };
            }

            // Temel sorgu — webe_gonderilecek_fl=1 karşılığı: varsayılan olarak sadece Aktif=true
            // SadeceAktif=false geçilirse pasifler de gösterilebilir (admin özel kullanımı)
            IQueryable<MikroProductCache> baseQuery = _context.MikroProductCaches;

            // Aktif/Pasif filtresi: null veya true → sadece aktif (webe=1), false → sadece pasif
            if (query.SadeceAktif == false)
                baseQuery = baseQuery.Where(p => !p.Aktif);
            else
                baseQuery = baseQuery.Where(p => p.Aktif); // null veya true: sadece webe_gonderilecek_fl=1 ürünler

            if (!string.IsNullOrWhiteSpace(query.StokKodFilter))
                baseQuery = baseQuery.Where(p => p.StokKod.Contains(query.StokKodFilter));

            if (!string.IsNullOrWhiteSpace(query.GrupKodFilter))
                baseQuery = baseQuery.Where(p => p.GrupKod != null && p.GrupKod.Contains(query.GrupKodFilter));

            if (!string.IsNullOrWhiteSpace(query.SearchTerm))
            {
                var term = query.SearchTerm.ToLower();
                baseQuery = baseQuery.Where(p =>
                    p.StokKod.ToLower().Contains(term) ||
                    (p.StokAd != null && p.StokAd.ToLower().Contains(term)) ||
                    (p.Barkod != null && p.Barkod.Contains(term)));
            }

            if (query.SadeceStokluOlanlar == true)
                baseQuery = baseQuery.Where(p => p.DepoMiktari > 0);
            else if (query.SadeceStokluOlanlar == false)
                baseQuery = baseQuery.Where(p => p.DepoMiktari <= 0);

            // Toplam sayı (filtreli)
            var totalCount = await baseQuery.CountAsync();

            // Sıralama
            baseQuery = query.SortBy?.ToLower() switch
            {
                "stokad" => query.SortDescending
                    ? baseQuery.OrderByDescending(p => p.StokAd)
                    : baseQuery.OrderBy(p => p.StokAd),
                "satisfiyati" => query.SortDescending
                    ? baseQuery.OrderByDescending(p => p.SatisFiyati)
                    : baseQuery.OrderBy(p => p.SatisFiyati),
                "depomiktari" => query.SortDescending
                    ? baseQuery.OrderByDescending(p => p.DepoMiktari)
                    : baseQuery.OrderBy(p => p.DepoMiktari),
                "guncellemeTarihi" => query.SortDescending
                    ? baseQuery.OrderByDescending(p => p.GuncellemeTarihi)
                    : baseQuery.OrderBy(p => p.GuncellemeTarihi),
                "grupkod" => query.SortDescending
                    ? baseQuery.OrderByDescending(p => p.GrupKod)
                    : baseQuery.OrderBy(p => p.GrupKod),
                _ => query.SortDescending
                    ? baseQuery.OrderByDescending(p => p.StokKod)
                    : baseQuery.OrderBy(p => p.StokKod)
            };

            // Sayfalama
            var items = await baseQuery
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();

            // Son sync zamanı
            var lastSync = await _context.MikroSyncStates
                .Where(s => s.SyncType == "ProductCache")
                .Select(s => s.LastSyncTime)
                .FirstOrDefaultAsync();

            sw.Stop();
            _logger.LogDebug(
                "[MikroProductCacheService] GetCachedProductsAsync - Sayfa {Page}, {Count}/{Total} ürün, {Ms}ms",
                query.Page, items.Count, totalCount, sw.ElapsedMilliseconds);

            return new MikroCachePageResult
            {
                Items = items,
                TotalCount = totalCount,
                Page = query.Page,
                PageSize = query.PageSize,
                LastSyncTime = lastSync
            };
        }

        /// <summary>
        /// Tüm web-aktif ürünleri Mikro'dan TEK SQL sorgusuyla çeker ve cache'e kaydeder.
        /// 
        /// YENİ AKIŞ (Unified SQL):
        /// 1. GetUnifiedProductMapAsync() → tek SQL ile fiyat + stok + bilgi
        /// 2. DirectMapToCache() → doğrudan 1:1 mapping (fallback chain yok)
        /// 3. Hash karşılaştırması → sadece değişenler güncellenir
        /// 
        /// ESKİ AKIŞ (devre dışı): StokListesiV2 sayfalı + GetSqlPriceMapAsync + GetSqlStockMapAsync
        /// </summary>
        public async Task<MikroCacheSyncResult> FetchAllAndCacheAsync(
            int fiyatListesiNo = 1,
            int depoNo = 0,
            IProgress<MikroFetchProgress>? progress = null)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var result = new MikroCacheSyncResult();

            try
            {
                _logger.LogInformation(
                    "[MikroProductCacheService] FetchAllAndCacheAsync başlıyor (Unified SQL). FiyatListesi: {FiyatListesi}, Depo: {Depo}",
                    fiyatListesiNo, depoNo);

                // İlerleme bildirimi — başladık
                progress?.Report(new MikroFetchProgress
                {
                    CurrentPage = 0,
                    TotalPages = 1,
                    FetchedCount = 0,
                    TotalCount = 0,
                    ElapsedTime = sw.Elapsed,
                    Status = "Mikro ERP'den ürünler çekiliyor (tek SQL sorgusu)..."
                });

                // TEK SQL SORGUSU — fiyat + stok + ürün bilgisi + barkod hepsi birden
                var unifiedProducts = await _mikroApiService.GetUnifiedProductMapAsync(
                    fiyatListesiNo,
                    depoNo > 0 ? depoNo : null);

                if (unifiedProducts.Count == 0)
                {
                    _logger.LogWarning("[MikroProductCacheService] Birleşik SQL sorgusu 0 ürün döndü.");
                    result.Success = false;
                    result.Message = "Mikro ERP'den ürün çekilemedi. SQL sorgusu boş sonuç döndü.";
                    result.Duration = sw.Elapsed;
                    return result;
                }

                _logger.LogInformation(
                    "[MikroProductCacheService] SQL'den {Count} web-aktif ürün çekildi. Fiyat>0: {PriceOk}, Stok>0: {StockOk}",
                    unifiedProducts.Count,
                    unifiedProducts.Count(p => p.Fiyat > 0),
                    unifiedProducts.Count(p => p.StokMiktar > 0));

                // Mevcut cache'i yükle — hash karşılaştırması için
                var existingProducts = await _context.MikroProductCaches
                    .ToDictionaryAsync(p => p.StokKod, p => p, StringComparer.OrdinalIgnoreCase);

                var newProducts = new List<MikroProductCache>();
                int zeroPriceCount = 0;

                foreach (var item in unifiedProducts)
                {
                    if (string.IsNullOrWhiteSpace(item.StokKod))
                        continue;

                    var cacheItem = DirectMapToCache(item, fiyatListesiNo, depoNo);

                    // Fiyat = 0 uyarısı — logla ama ürünü yine de cache'e al
                    if (cacheItem.SatisFiyati <= 0)
                    {
                        zeroPriceCount++;
                        if (zeroPriceCount <= 10) // İlk 10 tanesini logla (spam önleme)
                        {
                            _logger.LogWarning(
                                "[MikroProductCacheService] Fiyat=0 ürün: {StokKod} ({StokAd})",
                                cacheItem.StokKod, cacheItem.StokAd);
                        }
                    }

                    if (existingProducts.TryGetValue(cacheItem.StokKod, out var existing))
                    {
                        // Mevcut ürün — hash kontrolü ile güncelleme kararı
                        if (existing.DataHash != cacheItem.DataHash)
                        {
                            existing.StokAd = cacheItem.StokAd;
                            existing.Barkod = cacheItem.Barkod;
                            existing.GrupKod = cacheItem.GrupKod;
                            existing.AnagrupKod = cacheItem.AnagrupKod;
                            existing.Birim = cacheItem.Birim;
                            existing.KdvOrani = cacheItem.KdvOrani;
                            existing.SatisFiyati = cacheItem.SatisFiyati;
                            existing.FiyatListesiNo = cacheItem.FiyatListesiNo;
                            existing.DepoMiktari = cacheItem.DepoMiktari;
                            existing.SatilabilirMiktar = cacheItem.SatilabilirMiktar;
                            existing.DepoNo = cacheItem.DepoNo;
                            existing.TumFiyatlarJson = null;  // Birleşik sorguda tekil fiyat gelir
                            existing.TumDepolarJson = null;   // Birleşik sorguda tekil depo gelir
                            existing.DataHash = cacheItem.DataHash;
                            existing.GuncellemeTarihi = DateTime.UtcNow;
                            existing.Aktif = true;
                            existing.VeriKaynagi = "SQL_UNIFIED";
                            existing.SonHareketTarihi = cacheItem.SonHareketTarihi;
                            result.UpdatedProducts++;
                        }
                        else
                        {
                            result.UnchangedProducts++;
                        }
                    }
                    else
                    {
                        // Yeni ürün
                        newProducts.Add(cacheItem);
                        result.NewProducts++;
                    }

                    result.TotalFetched++;
                }

                // Mikro'dan artık gelmeyen ürünleri deaktif et (CSV export'ta görünmesin)
                // NEDEN: Ürün Mikro'da web-aktif listeden çıkarılmışsa cache'de Aktif=true kalında
                // export'ta yanlış/fazla ürün görünüyor. Bu adım cache'i Mikro ile senkron tutar.
                var fetchedStokKods = new HashSet<string>(
                    unifiedProducts.Select(p => p.StokKod ?? string.Empty),
                    StringComparer.OrdinalIgnoreCase);

                int deactivatedCount = 0;
                foreach (var cached in existingProducts.Values)
                {
                    if (cached.Aktif && !fetchedStokKods.Contains(cached.StokKod))
                    {
                        cached.Aktif = false;
                        cached.GuncellemeTarihi = DateTime.UtcNow;
                        deactivatedCount++;
                    }
                }

                if (deactivatedCount > 0)
                {
                    _logger.LogInformation(
                        "[MikroProductCacheService] {Count} ürün Mikro'dan artık gelmediği için deaktif edildi.",
                        deactivatedCount);
                    result.DeletedProducts = deactivatedCount;
                }

                // Yeni ürünleri batch olarak ekle
                if (newProducts.Count > 0)
                {
                    // Batch insert — büyük listeler için parçalara böl
                    for (int i = 0; i < newProducts.Count; i += BATCH_INSERT_SIZE)
                    {
                        var batch = newProducts.Skip(i).Take(BATCH_INSERT_SIZE).ToList();
                        await _context.MikroProductCaches.AddRangeAsync(batch);
                        await _context.SaveChangesAsync();
                    }
                }
                else
                {
                    // Güncelleme (update/deaktivasyon) varsa save et
                    await _context.SaveChangesAsync();
                }

                if (zeroPriceCount > 10)
                {
                    _logger.LogWarning(
                        "[MikroProductCacheService] Toplam {Count} ürünün fiyatı 0. İlk 10 tanesi loglandı.",
                        zeroPriceCount);
                }

                // İlerleme bildirimi — tamamlandı
                progress?.Report(new MikroFetchProgress
                {
                    CurrentPage = 1,
                    TotalPages = 1,
                    FetchedCount = result.TotalFetched,
                    TotalCount = result.TotalFetched,
                    ElapsedTime = sw.Elapsed,
                    Status = "Tamamlandı"
                });

                // Sync state güncelle
                await UpdateSyncStateAsync();

                // ADIM 5: Cache sync sonrası eşlenmemiş grup kodlarını otomatik eşle
                // NEDEN: Mikro'dan yeni ürün grupları geldiğinde mapping tablosu eksik olabilir.
                // Bu adım yeni grupları fuzzy match ile kategorilere eşler.
                if (_autoMappingEngine != null && (result.NewProducts > 0 || result.UpdatedProducts > 0))
                {
                    try
                    {
                        _logger.LogInformation(
                            "[MikroProductCacheService] Otomatik kategori eşleme başlatılıyor...");
                        var autoResult = await _autoMappingEngine.DiscoverAndMapAllAsync();
                        _logger.LogInformation(
                            "[MikroProductCacheService] Otomatik eşleme tamamlandı. " +
                            "Yeni mapping: {New}, Yeni kategori: {NewCat}, Diğer: {Diger}",
                            autoResult.NewMappingsCreated, autoResult.NewCategoriesCreated,
                            autoResult.FallbackToDiger);
                    }
                    catch (Exception autoEx)
                    {
                        // Otomatik eşleme hatası cache sync'i etkilememeli
                        _logger.LogWarning(autoEx,
                            "[MikroProductCacheService] Otomatik kategori eşleme hatası (cache sync başarılı)");
                    }
                }

                sw.Stop();
                result.Success = true;
                result.Duration = sw.Elapsed;
                result.Message = $"{result.TotalFetched} web-aktif ürün işlendi (Unified SQL). " +
                    $"Yeni: {result.NewProducts}, Güncellenen: {result.UpdatedProducts}, " +
                    $"Değişmeyen: {result.UnchangedProducts}, Deaktif: {result.DeletedProducts}, Fiyat=0: {zeroPriceCount}";

                _logger.LogInformation(
                    "[MikroProductCacheService] FetchAllAndCacheAsync tamamlandı. {Message} Süre: {Duration}",
                    result.Message, result.Duration);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MikroProductCacheService] FetchAllAndCacheAsync hatası");
                result.Success = false;
                result.Message = $"Hata: {ex.Message}";
                result.Errors.Add(ex.ToString());
            }

            return result;
        }

        /// <summary>
        /// Sadece değişen ürünleri günceller (delta sync).
        /// Birleşik SQL sorgusu ile tüm web-aktif ürünleri çeker,
        /// hash karşılaştırmasıyla sadece değişenleri günceller.
        /// NEDEN: FetchAllAndCacheAsync zaten hash-bazlı delta mantığı içerir.
        /// </summary>
        public async Task<MikroCacheSyncResult> SyncChangedProductsAsync(int fiyatListesiNo = 1, int depoNo = 0)
        {
            return await FetchAllAndCacheAsync(fiyatListesiNo, depoNo);
        }

        /// <summary>
        /// Yeni ürün kontrolü yapar — birleşik SQL sorgusuyla çekilip
        /// mevcut cache ile karşılaştırılarak yeni ürünler eklenir, değişenler güncellenir.
        /// ESKİ MANTIK (StokListesiV2 probe) kaldırıldı — artık tek SQL yeterli.
        /// </summary>
        public async Task<MikroCacheSyncResult> SyncNewProductsOnlyAsync(int fiyatListesiNo = 1, int depoNo = 0)
        {
            // Birleşik SQL zaten hem yeni hem değişen ürünleri handle ediyor
            return await FetchAllAndCacheAsync(fiyatListesiNo, depoNo);
        }

        /// <summary>
        /// Cache istatistiklerini getirir
        /// </summary>
        public async Task<MikroCacheStats> GetCacheStatsAsync()
        {
            var stats = new MikroCacheStats();

            stats.TotalProducts = await _context.MikroProductCaches.CountAsync();
            stats.ActiveProducts = await _context.MikroProductCaches.CountAsync(p => p.Aktif);
            stats.InactiveProducts = stats.TotalProducts - stats.ActiveProducts;
            stats.ProductsWithStock = await _context.MikroProductCaches.CountAsync(p => p.DepoMiktari > 0);
            stats.ProductsWithoutStock = stats.TotalProducts - stats.ProductsWithStock;
            stats.SyncedProducts = await _context.MikroProductCaches.CountAsync(p => p.LocalProductId != null);
            stats.NotSyncedProducts = stats.TotalProducts - stats.SyncedProducts;

            stats.OldestRecord = await _context.MikroProductCaches
                .OrderBy(p => p.OlusturmaTarihi)
                .Select(p => p.OlusturmaTarihi)
                .FirstOrDefaultAsync();

            stats.NewestRecord = await _context.MikroProductCaches
                .OrderByDescending(p => p.GuncellemeTarihi)
                .Select(p => p.GuncellemeTarihi)
                .FirstOrDefaultAsync();

            stats.LastSyncTime = await _context.MikroSyncStates
                .Where(s => s.SyncType == "ProductCache")
                .Select(s => s.LastSyncTime)
                .FirstOrDefaultAsync();

            // Grup kodlarına göre dağılım (top 20)
            stats.ProductsByGrupKod = await _context.MikroProductCaches
                .Where(p => p.GrupKod != null)
                .GroupBy(p => p.GrupKod!)
                .Select(g => new { GrupKod = g.Key, Count = g.Count() })
                .OrderByDescending(g => g.Count)
                .Take(20)
                .ToDictionaryAsync(g => g.GrupKod, g => g.Count);

            return stats;
        }

        /// <summary>
        /// Cache'i tamamen temizler
        /// </summary>
        public async Task ClearCacheAsync()
        {
            _logger.LogWarning("[MikroProductCacheService] Cache temizleniyor...");
            
            await _context.Database.ExecuteSqlRawAsync("DELETE FROM MikroProductCache");
            
            // Sync state'i de sıfırla
            var syncState = await _context.MikroSyncStates
                .FirstOrDefaultAsync(s => s.SyncType == "ProductCache");
            if (syncState != null)
            {
                _context.MikroSyncStates.Remove(syncState);
                await _context.SaveChangesAsync();
            }

            _logger.LogInformation("[MikroProductCacheService] Cache temizlendi");
        }

        /// <summary>
        /// Tek ürünün aktif/pasif durumunu değiştirir.
        /// </summary>
        public async Task<bool> SetProductActiveStatusAsync(string stokKod, bool aktif)
        {
            if (string.IsNullOrWhiteSpace(stokKod))
            {
                return false;
            }

            var product = await _context.MikroProductCaches
                .FirstOrDefaultAsync(p => p.StokKod == stokKod);

            if (product == null)
            {
                _logger.LogWarning(
                    "[MikroProductCacheService] Ürün bulunamadı. StokKod: {StokKod}",
                    stokKod);
                return false;
            }

            product.Aktif = aktif;
            product.GuncellemeTarihi = DateTime.UtcNow;
            product.SyncStatus = (int)MikroSyncStatus.PendingUpdate; // Mikro'ya sync bekliyor

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "[MikroProductCacheService] Ürün aktiflik durumu değiştirildi. StokKod: {StokKod}, Aktif: {Aktif}",
                stokKod, aktif);

            return true;
        }

        /// <summary>
        /// Tüm cache'deki ürünleri getirir (export veya grup kodu listesi için)
        /// </summary>
        public async Task<IEnumerable<MikroProductCache>> GetAllAsync()
        {
            // Sadece Aktif=true kayıtlar döndürülür (Mikro'da webe_gonderilecek_fl=1 olanlar)
            return await _context.MikroProductCaches.Where(p => p.Aktif).ToListAsync();
        }

        /// <summary>
        /// Birleşik SQL sonucunu doğrudan cache entity'sine 1:1 map eder.
        /// 
        /// NEDEN: Eski ConvertToCache 6 kademeli fiyat + 4 kademeli stok fallback zinciri kullanıyordu.
        /// Artık tek SQL sorgusu hem fiyat hem stok veriyor → fallback gereksiz.
        /// Bu metod sade, hızlı ve tek kaynaklı.
        /// </summary>
        private MikroProductCache DirectMapToCache(
            MikroUnifiedProductDto item,
            int fiyatListesiNo,
            int depoNo)
        {
            var normalizedSku = (item.StokKod ?? string.Empty).Trim();

            // Hash hesaplama — değişiklik tespiti için (aynı format korunuyor: uyumluluk)
            var hashInput = $"{item.StokAd}|{item.Fiyat}|{item.StokMiktar}|{item.KdvOrani}";
            var dataHash = ComputeMd5Hash(hashInput);

            return new MikroProductCache
            {
                StokKod = normalizedSku,
                StokAd = item.StokAd,
                Barkod = item.Barkod,
                GrupKod = item.GrupKod,
                AnagrupKod = item.AnagrupKod,
                Birim = item.Birim,
                KdvOrani = item.KdvOrani,
                SatisFiyati = item.Fiyat,
                FiyatListesiNo = fiyatListesiNo,
                DepoMiktari = item.StokMiktar,
                SatilabilirMiktar = Math.Max(0, item.StokMiktar), // Unified SQL'de rezerve bilgisi yok, brüt=net
                DepoNo = item.DepoNo ?? depoNo,
                TumFiyatlarJson = null,   // Birleşik sorguda tekil fiyat gelir — multi-list JSON gereksiz
                TumDepolarJson = null,    // Birleşik sorguda tekil depo gelir — multi-depo JSON gereksiz
                OlusturmaTarihi = DateTime.UtcNow,
                GuncellemeTarihi = DateTime.UtcNow,
                Aktif = true,
                DataHash = dataHash,
                VeriKaynagi = "SQL_UNIFIED",
                SonHareketTarihi = item.SonHareketTarihi
            };
        }

        /// <summary>
        /// [LEGACY] Mikro StokListesiV2 API yanıtını cache entity'sine dönüştürür.
        /// Yeni akışta kullanılmaz — geriye dönük uyumluluk için saklanır.
        /// </summary>
        [Obsolete("Birleşik SQL akışı (DirectMapToCache) kullanın. Bu metod legacy StokListesiV2 akışı içindir.")]
        private MikroProductCache ConvertToCache(
            MikroStokResponseDto item,
            int fiyatListesiNo,
            int depoNo,
            IReadOnlyDictionary<string, MikroFiyatSatirDto> sqlPriceMap,
            IReadOnlyDictionary<string, decimal> sqlStockMap)
        {
            // ========== FİYAT HESAPLAMA (Multi-source fallback) ==========
            decimal satisFiyati = 0;

            var sku = item.StoKod ?? string.Empty;
            var normalizedSku = sku.Trim();
            
            // Kaynak 1: SqlVeriOkuV2'den gelen fiyat (en güvenilir kaynak)
            if (!string.IsNullOrWhiteSpace(normalizedSku) &&
                sqlPriceMap.TryGetValue(normalizedSku, out var sqlPrice) &&
                sqlPrice.Fiyat > 0)
            {
                satisFiyati = sqlPrice.Fiyat;
            }
            
            // Kaynak 2: StokListesiV2'deki SatisFiyatlari
            // Fiyat listesi seçilmemişse (0) ilk uygun fiyatı kullan.
            if (satisFiyati <= 0 && item.SatisFiyatlari != null && item.SatisFiyatlari.Any())
            {
                MikroStokFiyatResponseDto? selectedPrice = null;
                if (fiyatListesiNo > 0)
                {
                    selectedPrice = item.SatisFiyatlari.FirstOrDefault(f => f.SfiyatNo == fiyatListesiNo);
                }

                selectedPrice ??= item.SatisFiyatlari
                    .OrderBy(f => f.SfiyatNo)
                    .FirstOrDefault(f => f.SfiyatFiyati > 0);

                if (selectedPrice != null && selectedPrice.SfiyatFiyati > 0)
                {
                    satisFiyati = selectedPrice.SfiyatFiyati;
                }
            }

            // Kaynak 3: SatisFiyat1 helper property (perakende fiyatı)
            if (satisFiyati <= 0 && item.SatisFiyat1.HasValue && item.SatisFiyat1.Value > 0)
            {
                satisFiyati = item.SatisFiyat1.Value;
            }
            
            // Kaynak 4: StoSonAlis (son alış fiyatı - son çare)
            if (satisFiyati <= 0 && item.StoSonAlis.HasValue && item.StoSonAlis.Value > 0)
            {
                satisFiyati = item.StoSonAlis.Value;
                _logger.LogDebug(
                    "[MikroProductCacheService] {StokKod} için fiyat bulunamadı, StoSonAlis kullanıldı: {Fiyat}",
                    normalizedSku, satisFiyati);
            }

            // Kaynak 5: Ortalama maliyet (bazı kartlarda satış fiyatı boş gelebiliyor)
            if (satisFiyati <= 0 && item.StoOrtMaliyet.HasValue && item.StoOrtMaliyet.Value > 0)
            {
                satisFiyati = item.StoOrtMaliyet.Value;
            }

            // Kaynak 6: Alış fiyat listesi
            if (satisFiyati <= 0 && item.AlisFiyatlari != null && item.AlisFiyatlari.Any())
            {
                var alisFiyati = item.AlisFiyatlari
                    .OrderBy(f => f.SfiyatNo)
                    .FirstOrDefault(f => f.SfiyatFiyati > 0);

                if (alisFiyati != null && alisFiyati.SfiyatFiyati > 0)
                {
                    satisFiyati = alisFiyati.SfiyatFiyati;
                }
            }

            // ========== STOK MİKTARI HESAPLAMA (Multi-source fallback) ==========
            decimal depoMiktari = 0;
            decimal satilabilirMiktar = 0;
            var sqlSkuKey = normalizedSku;

            // Kaynak 0: SQL stok sorgusu (en güvenilir stok kaynağı)
            if (!string.IsNullOrWhiteSpace(sqlSkuKey) &&
                sqlStockMap.TryGetValue(sqlSkuKey, out var sqlStockQuantity) &&
                sqlStockQuantity > 0)
            {
                depoMiktari = sqlStockQuantity;
                satilabilirMiktar = sqlStockQuantity;
            }

            // Kaynak 1: Belirli depo seçilmişse o deponun stoğu
            if (depoMiktari <= 0 && depoNo > 0 && item.DepoStoklari != null && item.DepoStoklari.Any())
            {
                var depoStok = item.DepoStoklari.FirstOrDefault(d => d.DepNo == depoNo);
                if (depoStok != null)
                {
                    depoMiktari = depoStok.StokMiktar;
                    satilabilirMiktar = depoStok.SatilabilirMiktar ?? (depoStok.StokMiktar - (depoStok.RezerveMiktar ?? 0));
                }
            }
            // Kaynak 2: DepoMiktar veya StoMiktar
            else if (depoMiktari <= 0 && item.DepoMiktar.HasValue && item.DepoMiktar.Value > 0)
            {
                depoMiktari = item.DepoMiktar.Value;
                satilabilirMiktar = item.KullanilabilirMiktar ?? (depoMiktari - (item.RezerveMiktar ?? 0));
            }
            else if (depoMiktari <= 0 && item.StoMiktar > 0)
            {
                depoMiktari = item.StoMiktar;
                satilabilirMiktar = item.KullanilabilirMiktar ?? (depoMiktari - (item.RezerveMiktar ?? 0));
            }
            // Kaynak 3: Tüm depoların toplamı
            else if (depoMiktari <= 0 && item.DepoStoklari != null && item.DepoStoklari.Any())
            {
                depoMiktari = item.DepoStoklari.Sum(d => d.StokMiktar);
                satilabilirMiktar = item.DepoStoklari.Sum(d => d.SatilabilirMiktar ?? (d.StokMiktar - (d.RezerveMiktar ?? 0)));
            }

            // JSON serileştirme
            string? tumFiyatlarJson = null;
            if (item.SatisFiyatlari != null && item.SatisFiyatlari.Any())
            {
                tumFiyatlarJson = JsonSerializer.Serialize(
                    item.SatisFiyatlari.Select(f => new { no = f.SfiyatNo, fiyat = f.SfiyatFiyati }));
            }

            string? tumDepolarJson = null;
            if (item.DepoStoklari != null && item.DepoStoklari.Any())
            {
                tumDepolarJson = JsonSerializer.Serialize(
                    item.DepoStoklari.Select(d => new { depoNo = d.DepNo, miktar = d.StokMiktar }));
            }

            // Hash hesaplama (değişiklik tespiti için)
            var hashInput = $"{item.StoIsim}|{satisFiyati}|{depoMiktari}|{item.KdvOrani}";
            var dataHash = ComputeMd5Hash(hashInput);

            return new MikroProductCache
            {
                StokKod = item.StoKod ?? string.Empty,
                StokAd = item.StoIsim,
                Barkod = !string.IsNullOrWhiteSpace(item.Barkod)
                    ? item.Barkod
                    : (sqlPriceMap.TryGetValue(normalizedSku, out var sqlRow) ? sqlRow.Barkod : item.Barkod),
                GrupKod = item.GrupKodu,
                AnagrupKod = item.StoAnagrupKod,
                Birim = item.BirimAdi,
                KdvOrani = item.KdvOrani ?? 0,
                SatisFiyati = satisFiyati,
                FiyatListesiNo = fiyatListesiNo,
                DepoMiktari = depoMiktari,
                SatilabilirMiktar = Math.Max(0, satilabilirMiktar),
                DepoNo = depoNo,
                TumFiyatlarJson = tumFiyatlarJson,
                TumDepolarJson = tumDepolarJson,
                OlusturmaTarihi = DateTime.UtcNow,
                GuncellemeTarihi = DateTime.UtcNow,
                Aktif = true,
                DataHash = dataHash
            };
        }

        /// <summary>
        /// Verilen stok kodları için cache aktiflik durumunu map olarak döndürür.
        /// </summary>
        public async Task<Dictionary<string, bool>> GetActiveStatusMapByStokKodAsync(IEnumerable<string> stokKodlar)
        {
            var normalizedCodes = (stokKodlar ?? Enumerable.Empty<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (normalizedCodes.Count == 0)
            {
                return new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            }

            var rows = await _context.MikroProductCaches
                .AsNoTracking()
                .Where(p => normalizedCodes.Contains(p.StokKod))
                .Select(p => new { p.StokKod, p.Aktif })
                .ToListAsync();

            return rows
                .GroupBy(x => x.StokKod, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First().Aktif, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// MikroProductCache → Product tablosu senkronizasyonu.
        /// 
        /// NEDEN: Frontend Product tablosundan okur (ProductManager → ProductRepository).
        /// MikroProductCache güncellendiğinde bu senkronizasyon çalışmazsa
        /// Product.Price ve Product.StockQuantity eski değerde kalır → frontend 0 gösterir.
        /// 
        /// AKIŞ:
        /// 1. Aktif MikroProductCache kayıtlarını al
        /// 2. Product tablosunda SKU ile eşleştir
        /// 3. Fiyat veya stok değişmişse güncelle
        /// 4. Batch save ile DB'ye yaz
        /// </summary>
        public async Task<int> SyncCacheToProductTableAsync()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                // Aktif cache kayıtları — sadece fiyat veya stok bilgisi olanlar
                var cacheItems = await _context.MikroProductCaches
                    .AsNoTracking()
                    .Where(c => c.Aktif)
                    .Select(c => new { c.StokKod, c.SatisFiyati, c.DepoMiktari, c.SatilabilirMiktar })
                    .ToListAsync();

                if (cacheItems.Count == 0)
                {
                    _logger.LogInformation("[MikroProductCacheService] SyncCacheToProductTableAsync: Cache boş, atlanıyor.");
                    return 0;
                }

                // SKU bazlı cache map — hızlı lookup için
                var cacheMap = cacheItems
                    .GroupBy(c => c.StokKod, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(
                        g => g.Key,
                        g => g.First(),
                        StringComparer.OrdinalIgnoreCase);

                // Product tablosu — sadece SKU'su cache'de olanları çek (performans)
                var skuList = cacheMap.Keys.ToList();
                var products = await _context.Products
                    .Where(p => skuList.Contains(p.SKU))
                    .ToListAsync();

                int updatedCount = 0;

                foreach (var product in products)
                {
                    if (string.IsNullOrWhiteSpace(product.SKU))
                        continue;

                    if (!cacheMap.TryGetValue(product.SKU, out var cache))
                        continue;

                    bool changed = false;

                    // Fiyat güncellemesi — cache'den gelen fiyat > 0 ise güncelle
                    // NEDEN > 0 kontrolü: 0 fiyatlı ürünler teknik hata olabilir, mevcut fiyatı korumak daha güvenli
                    if (cache.SatisFiyati > 0 && product.Price != cache.SatisFiyati)
                    {
                        product.Price = cache.SatisFiyati;
                        changed = true;
                    }

                    // Stok güncellemesi — satılabilir miktar öncelikli, yoksa depo miktarı
                    var newStock = (int)Math.Max(0, Math.Floor(cache.SatilabilirMiktar > 0
                        ? cache.SatilabilirMiktar
                        : cache.DepoMiktari));

                    if (product.StockQuantity != newStock)
                    {
                        product.StockQuantity = newStock;
                        changed = true;
                    }

                    if (changed)
                        updatedCount++;
                }

                if (updatedCount > 0)
                {
                    await _context.SaveChangesAsync();
                }

                sw.Stop();
                _logger.LogInformation(
                    "[MikroProductCacheService] SyncCacheToProductTableAsync tamamlandı. " +
                    "Product güncellenen: {Updated}/{Total}, Süre: {Duration}ms",
                    updatedCount, products.Count, sw.ElapsedMilliseconds);

                return updatedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MikroProductCacheService] SyncCacheToProductTableAsync hatası");
                return 0;
            }
        }

        /// <summary>
        /// MD5 hash hesapla
        /// </summary>
        private static string ComputeMd5Hash(string input)
        {
            using var md5 = MD5.Create();
            var inputBytes = Encoding.UTF8.GetBytes(input);
            var hashBytes = md5.ComputeHash(inputBytes);
            return Convert.ToHexString(hashBytes).ToLowerInvariant();
        }

        /// <summary>
        /// Sync state tablosunu güncelle
        /// </summary>
        private async Task UpdateSyncStateAsync()
        {
            var syncState = await _context.MikroSyncStates
                .FirstOrDefaultAsync(s => s.SyncType == "ProductCache");

            if (syncState == null)
            {
                syncState = new MikroSyncState
                {
                    SyncType = "ProductCache",
                    Direction = "FromERP",
                    IsEnabled = true
                };
                _context.MikroSyncStates.Add(syncState);
            }

            syncState.LastSyncTime = DateTime.UtcNow;
            syncState.LastSyncSuccess = true;
            syncState.LastSyncCount = await _context.MikroProductCaches.CountAsync();
            syncState.ConsecutiveFailures = 0;

            await _context.SaveChangesAsync();
        }
    }
}
