using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ECommerce.Data.Context;
using ECommerce.Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ECommerce.API.Services
{
    /// <summary>
    /// Mikro ERP ürün cache yönetim servisi.
    /// 
    /// AMAÇ:
    /// - 6000+ ürünü tek seferde çekip local DB'ye cache'ler
    /// - Sonraki isteklerde milisaniyeler içinde yanıt verir
    /// - Delta sync ile sadece değişen ürünleri günceller
    /// 
    /// KULLANIM SENARYOLARI:
    /// 1. İlk çekim: FetchAllAndCacheAsync() - tüm ürünleri çeker ve cache'ler (1 kez)
    /// 2. Sayfa görüntüleme: GetCachedProductsAsync() - cache'den hızlı okuma
    /// 3. Yenileme: SyncChangedProductsAsync() - sadece değişenleri günceller
    /// </summary>
    public interface IMikroProductCacheService
    {
        /// <summary>
        /// Cache'deki ürünleri sayfalı olarak getirir (çok hızlı - local DB)
        /// </summary>
        Task<MikroCachePageResult> GetCachedProductsAsync(MikroCacheQuery query);
        
        /// <summary>
        /// Tüm ürünleri Mikro'dan çeker ve cache'e kaydeder (ilk çekim)
        /// </summary>
        Task<MikroCacheSyncResult> FetchAllAndCacheAsync(int fiyatListesiNo = 1, int depoNo = 0, IProgress<MikroFetchProgress>? progress = null);
        
        /// <summary>
        /// Sadece değişen ürünleri günceller (delta sync)
        /// </summary>
        Task<MikroCacheSyncResult> SyncChangedProductsAsync(int fiyatListesiNo = 1, int depoNo = 0);

        /// <summary>
        /// Sadece yeni ürün varsa senkronizasyon yapar.
        /// Toplam ürün adedi değişmediyse ERP çağrısı atlaması yapar.
        /// </summary>
        Task<MikroCacheSyncResult> SyncNewProductsOnlyAsync(int fiyatListesiNo = 1, int depoNo = 0);
        
        /// <summary>
        /// Cache istatistiklerini getirir
        /// </summary>
        Task<MikroCacheStats> GetCacheStatsAsync();
        
        /// <summary>
        /// Cache'i tamamen temizler
        /// </summary>
        Task ClearCacheAsync();
    }

    /// <summary>
    /// Cache sorgu parametreleri
    /// </summary>
    public class MikroCacheQuery
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
        public string? StokKodFilter { get; set; }
        public string? GrupKodFilter { get; set; }
        public string? SearchTerm { get; set; }
        public bool? SadeceStokluOlanlar { get; set; }
        public bool SadeceAktif { get; set; } = true;
        public string? SortBy { get; set; } = "StokKod";
        public bool SortDescending { get; set; } = false;
    }

    /// <summary>
    /// Sayfalı cache sonucu
    /// </summary>
    public class MikroCachePageResult
    {
        public List<MikroProductCache> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
        public bool HasPreviousPage => Page > 1;
        public bool HasNextPage => Page < TotalPages;
        public DateTime? LastSyncTime { get; set; }
    }

    /// <summary>
    /// Sync işlem sonucu
    /// </summary>
    public class MikroCacheSyncResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public int TotalFetched { get; set; }
        public int NewProducts { get; set; }
        public int UpdatedProducts { get; set; }
        public int DeletedProducts { get; set; }
        public int UnchangedProducts { get; set; }
        public TimeSpan Duration { get; set; }
        public List<string> Errors { get; set; } = new();
    }

    /// <summary>
    /// Cache istatistikleri
    /// </summary>
    public class MikroCacheStats
    {
        public int TotalProducts { get; set; }
        public int ActiveProducts { get; set; }
        public int InactiveProducts { get; set; }
        public int ProductsWithStock { get; set; }
        public int ProductsWithoutStock { get; set; }
        public int SyncedProducts { get; set; }
        public int NotSyncedProducts { get; set; }
        public DateTime? LastSyncTime { get; set; }
        public DateTime? OldestRecord { get; set; }
        public DateTime? NewestRecord { get; set; }
        public Dictionary<string, int> ProductsByGrupKod { get; set; } = new();
    }

    /// <summary>
    /// Fetch ilerleme bilgisi (progress reporting)
    /// </summary>
    public class MikroFetchProgress
    {
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int FetchedCount { get; set; }
        public int TotalCount { get; set; }
        public double ProgressPercentage => TotalPages > 0 ? (double)CurrentPage / TotalPages * 100 : 0;
        public TimeSpan ElapsedTime { get; set; }
        public TimeSpan? EstimatedRemainingTime { get; set; }
        public string Status { get; set; } = "Çekiliyor...";
    }

    /// <summary>
    /// Mikro ürün cache servisi implementasyonu
    /// </summary>
    public class MikroProductCacheService : IMikroProductCacheService
    {
        private readonly ECommerceDbContext _context;
        private readonly IMikroApiService _mikroApiService;
        private readonly ILogger<MikroProductCacheService> _logger;

        // Batch işlem boyutları - performans optimizasyonu
        private const int FETCH_PAGE_SIZE = 500;      // Mikro'dan çekilecek sayfa boyutu
        private const int BATCH_INSERT_SIZE = 100;    // DB'ye yazılacak batch boyutu
        private const int THROTTLE_DELAY_MS = 100;    // API istekleri arası bekleme

        public MikroProductCacheService(
            ECommerceDbContext context,
            IMikroApiService mikroApiService,
            ILogger<MikroProductCacheService> logger)
        {
            _context = context;
            _mikroApiService = mikroApiService;
            _logger = logger;
        }

        /// <summary>
        /// Cache'den sayfalı ürün listesi getirir - ÇOK HIZLI (local DB)
        /// </summary>
        public async Task<MikroCachePageResult> GetCachedProductsAsync(MikroCacheQuery query)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();

            // Temel sorgu
            IQueryable<MikroProductCache> baseQuery = _context.MikroProductCaches;

            // Filtreler
            if (query.SadeceAktif)
                baseQuery = baseQuery.Where(p => p.Aktif);

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
        /// Tüm ürünleri Mikro'dan çeker ve cache'e kaydeder
        /// </summary>
        public async Task<MikroCacheSyncResult> FetchAllAndCacheAsync(
            int fiyatListesiNo = 1,
            int depoNo = 0,
            IProgress<MikroFetchProgress>? progress = null)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var result = new MikroCacheSyncResult();
            var allProducts = new List<MikroProductCache>();

            try
            {
                _logger.LogInformation(
                    "[MikroProductCacheService] FetchAllAndCacheAsync başlıyor. FiyatListesi: {FiyatListesi}, Depo: {Depo}",
                    fiyatListesiNo, depoNo);

                int currentPage = 1;
                int totalCount = 0;
                int totalPages = 1;
                var existingProducts = await _context.MikroProductCaches
                    .ToDictionaryAsync(p => p.StokKod, p => p);

                // Sayfa sayfa çek
                while (currentPage <= totalPages)
                {
                    var request = new MikroStokListesiRequestDto
                    {
                        SayfaNo = currentPage,
                        SayfaBuyuklugu = FETCH_PAGE_SIZE,
                        DepoNo = depoNo > 0 ? depoNo : null,
                        FiyatDahil = true,
                        BarkodDahil = true,
                        PasifDahil = false
                    };

                    var response = await _mikroApiService.GetStokListesiV2Async(request);

                    if (!response.Success)
                    {
                        result.Errors.Add($"Sayfa {currentPage}: {response.Message}");
                        _logger.LogWarning("[MikroProductCacheService] Sayfa {Page} başarısız: {Message}",
                            currentPage, response.Message);
                        currentPage++;
                        continue;
                    }

                    // İlk sayfada toplam bilgilerini al
                    if (currentPage == 1 && response.TotalCount.HasValue)
                    {
                        totalCount = response.TotalCount.Value;
                        totalPages = (int)Math.Ceiling((double)totalCount / FETCH_PAGE_SIZE);
                        _logger.LogInformation(
                            "[MikroProductCacheService] Toplam {Total} ürün, {Pages} sayfa bulundu",
                            totalCount, totalPages);
                    }

                    // Ürünleri dönüştür
                    foreach (var item in response.Data ?? Enumerable.Empty<MikroStokResponseDto>())
                    {
                        var cacheItem = ConvertToCache(item, fiyatListesiNo, depoNo);

                        if (existingProducts.TryGetValue(cacheItem.StokKod, out var existing))
                        {
                            // Mevcut ürün - hash kontrolü
                            if (existing.DataHash != cacheItem.DataHash)
                            {
                                // Değişmiş - güncelle
                                existing.StokAd = cacheItem.StokAd;
                                existing.Barkod = cacheItem.Barkod;
                                existing.GrupKod = cacheItem.GrupKod;
                                existing.Birim = cacheItem.Birim;
                                existing.KdvOrani = cacheItem.KdvOrani;
                                existing.SatisFiyati = cacheItem.SatisFiyati;
                                existing.FiyatListesiNo = cacheItem.FiyatListesiNo;
                                existing.DepoMiktari = cacheItem.DepoMiktari;
                                existing.SatilabilirMiktar = cacheItem.SatilabilirMiktar;
                                existing.DepoNo = cacheItem.DepoNo;
                                existing.TumFiyatlarJson = cacheItem.TumFiyatlarJson;
                                existing.TumDepolarJson = cacheItem.TumDepolarJson;
                                existing.DataHash = cacheItem.DataHash;
                                existing.GuncellemeTarihi = DateTime.UtcNow;
                                existing.Aktif = true;
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
                            allProducts.Add(cacheItem);
                            result.NewProducts++;
                        }

                        result.TotalFetched++;
                    }

                    // Progress raporu
                    if (progress != null)
                    {
                        var elapsed = sw.Elapsed;
                        var avgTimePerPage = elapsed.TotalMilliseconds / currentPage;
                        var remaining = TimeSpan.FromMilliseconds(avgTimePerPage * (totalPages - currentPage));

                        progress.Report(new MikroFetchProgress
                        {
                            CurrentPage = currentPage,
                            TotalPages = totalPages,
                            FetchedCount = result.TotalFetched,
                            TotalCount = totalCount,
                            ElapsedTime = elapsed,
                            EstimatedRemainingTime = remaining,
                            Status = $"Sayfa {currentPage}/{totalPages} işleniyor..."
                        });
                    }

                    currentPage++;

                    // Rate limiting
                    if (currentPage <= totalPages)
                        await Task.Delay(THROTTLE_DELAY_MS);
                }

                // Yeni ürünleri batch halinde ekle
                if (allProducts.Any())
                {
                    _logger.LogInformation(
                        "[MikroProductCacheService] {Count} yeni ürün ekleniyor...",
                        allProducts.Count);

                    foreach (var batch in allProducts.Chunk(BATCH_INSERT_SIZE))
                    {
                        await _context.MikroProductCaches.AddRangeAsync(batch);
                        await _context.SaveChangesAsync();
                    }
                }

                // Değişiklikleri kaydet
                await _context.SaveChangesAsync();

                // Sync state güncelle
                await UpdateSyncStateAsync();

                sw.Stop();
                result.Success = true;
                result.Duration = sw.Elapsed;
                result.Message = $"{result.TotalFetched} ürün işlendi. " +
                    $"Yeni: {result.NewProducts}, Güncellenen: {result.UpdatedProducts}, " +
                    $"Değişmeyen: {result.UnchangedProducts}";

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
        /// Sadece değişen ürünleri günceller (delta sync)
        /// </summary>
        public async Task<MikroCacheSyncResult> SyncChangedProductsAsync(int fiyatListesiNo = 1, int depoNo = 0)
        {
            // Şimdilik full sync ile aynı - Mikro API delta desteği varsa implement edilebilir
            return await FetchAllAndCacheAsync(fiyatListesiNo, depoNo);
        }

        /// <summary>
        /// Yeni ürün kontrolü yapar, yeni ürün yoksa full fetch'i atlar.
        /// </summary>
        public async Task<MikroCacheSyncResult> SyncNewProductsOnlyAsync(int fiyatListesiNo = 1, int depoNo = 0)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var result = new MikroCacheSyncResult();

            try
            {
                var localCount = await _context.MikroProductCaches.CountAsync(p => p.Aktif);

                var probeRequest = new MikroStokListesiRequestDto
                {
                    SayfaNo = 1,
                    SayfaBuyuklugu = 1,
                    DepoNo = depoNo > 0 ? depoNo : null,
                    FiyatDahil = true,
                    BarkodDahil = true,
                    PasifDahil = false
                };

                var probeResponse = await _mikroApiService.GetStokListesiV2Async(probeRequest);
                if (!probeResponse.Success)
                {
                    result.Success = false;
                    result.Message = probeResponse.Message ?? "Mikro ERP ürün sayısı kontrolü başarısız";
                    if (!string.IsNullOrWhiteSpace(probeResponse.Message))
                    {
                        result.Errors.Add(probeResponse.Message!);
                    }
                    return result;
                }

                var remoteCount = Math.Max(0, probeResponse.TotalCount ?? 0);
                if (remoteCount <= localCount)
                {
                    sw.Stop();
                    result.Success = true;
                    result.Duration = sw.Elapsed;
                    result.TotalFetched = 0;
                    result.NewProducts = 0;
                    result.UpdatedProducts = 0;
                    result.UnchangedProducts = localCount;
                    result.Message = $"Yeni ürün bulunmadı. Local: {localCount}, ERP: {remoteCount}.";
                    _logger.LogInformation(
                        "[MikroProductCacheService] SyncNewProductsOnlyAsync atlandı. Local: {Local}, ERP: {Remote}",
                        localCount, remoteCount);
                    return result;
                }

                _logger.LogInformation(
                    "[MikroProductCacheService] Yeni ürün tespit edildi. Local: {Local}, ERP: {Remote}. Full delta sync başlatılıyor.",
                    localCount, remoteCount);

                return await FetchAllAndCacheAsync(fiyatListesiNo, depoNo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MikroProductCacheService] SyncNewProductsOnlyAsync hatası");
                result.Success = false;
                result.Message = $"Hata: {ex.Message}";
                result.Errors.Add(ex.ToString());
                return result;
            }
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
        /// Mikro API yanıtını cache entity'sine dönüştürür
        /// </summary>
        private MikroProductCache ConvertToCache(MikroStokResponseDto item, int fiyatListesiNo, int depoNo)
        {
            // Fiyat hesaplama
            decimal satisFiyati = 0;
            if (item.SatisFiyatlari != null && item.SatisFiyatlari.Any())
            {
                var selectedPrice = item.SatisFiyatlari.FirstOrDefault(f => f.SfiyatNo == fiyatListesiNo);
                if (selectedPrice != null && selectedPrice.SfiyatFiyati > 0)
                {
                    satisFiyati = selectedPrice.SfiyatFiyati;
                }
                else
                {
                    // Fallback: İlk pozitif fiyat
                    var firstPositive = item.SatisFiyatlari.FirstOrDefault(f => f.SfiyatFiyati > 0);
                    if (firstPositive != null)
                        satisFiyati = firstPositive.SfiyatFiyati;
                }
            }

            // Stok miktarı hesaplama
            decimal depoMiktari = 0;
            decimal satilabilirMiktar = 0;

            if (depoNo > 0 && item.DepoStoklari != null)
            {
                var depoStok = item.DepoStoklari.FirstOrDefault(d => d.DepoNo == depoNo);
                if (depoStok != null)
                {
                    depoMiktari = depoStok.Miktar;
                    satilabilirMiktar = depoStok.Miktar - (depoStok.RezerveMiktar ?? 0);
                }
            }
            else
            {
                // Tüm depolar
                depoMiktari = item.StoMiktar ?? item.DepoMiktar ?? 0;
                satilabilirMiktar = depoMiktari - (item.RezerveMiktar ?? 0);
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
                    item.DepoStoklari.Select(d => new { depoNo = d.DepoNo, miktar = d.Miktar }));
            }

            // Hash hesaplama (değişiklik tespiti için)
            var hashInput = $"{item.StoIsim}|{satisFiyati}|{depoMiktari}|{item.KdvOrani}";
            var dataHash = ComputeMd5Hash(hashInput);

            return new MikroProductCache
            {
                StokKod = item.StoKod ?? string.Empty,
                StokAd = item.StoIsim,
                Barkod = item.Barkod,
                GrupKod = item.GrupKodu,
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
