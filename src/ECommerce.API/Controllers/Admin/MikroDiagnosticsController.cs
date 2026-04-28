using ECommerce.Core.Constants;
using ECommerce.Core.Interfaces.Cache;
using ECommerce.Data.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ECommerce.API.Controllers.Admin
{
    /// <summary>
    /// FAZA 8.3 — Mikro Birleşik Sync Diagnostics Endpoint'leri (Admin Only).
    ///
    /// AMAÇ:
    /// - Canlı sistemde unified sync'in doğru çalışıp çalışmadığını doğrulamak
    /// - Cache ↔ Product tablosu tutarlılığını kontrol etmek
    /// - Manuel sync tetikleme (test amaçlı)
    /// - Performans karşılaştırma verisi toplamak
    ///
    /// ERİŞİM: Sadece Admin/SuperAdmin/StoreManager (Roles.AdminLike) rolüne sahip kullanıcılar.
    /// </summary>
    [ApiController]
    [Route("api/admin/mikro-diagnostics")]
    [Authorize(Roles = Roles.AdminLike)]
    public class MikroDiagnosticsController : ControllerBase
    {
        private readonly IMikroProductCacheService _cacheService;
        private readonly ECommerceDbContext _context;
        private readonly ILogger<MikroDiagnosticsController> _logger;

        public MikroDiagnosticsController(
            IMikroProductCacheService cacheService,
            ECommerceDbContext context,
            ILogger<MikroDiagnosticsController> logger)
        {
            _cacheService = cacheService;
            _context = context;
            _logger = logger;
        }

        // ==================== 8.1: CACHE DURUM RAPORU ====================

        /// <summary>
        /// FAZA 8.1/8.2 — Cache ve Product tablosu senkronizasyon durumu.
        ///
        /// Döndürülenler:
        /// - MikroProductCache toplam kayıt
        /// - Aktif/pasif cache sayısı
        /// - SQL_UNIFIED kaynağından gelen sayısı
        /// - Product tablosunda fiyat/stok tutarsızlığı raporu
        /// - Son sync zamanı
        ///
        /// GET /api/admin/mikro-diagnostics/status
        /// </summary>
        [HttpGet("status")]
        public async Task<IActionResult> GetSyncStatus()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                // 1. Cache istatistikleri
                var cacheStats = await _cacheService.GetCacheStatsAsync();

                // 2. Cache tablosu dökümü
                var cacheTotal = await _context.MikroProductCaches.CountAsync();
                var cacheActive = await _context.MikroProductCaches.CountAsync(c => c.Aktif);
                var cacheInactive = cacheTotal - cacheActive;
                var cacheUnifiedSource = await _context.MikroProductCaches
                    .CountAsync(c => c.VeriKaynagi == "SQL_UNIFIED");
                var cacheLegacySource = cacheTotal - cacheUnifiedSource;
                var cacheZeroPriceCount = await _context.MikroProductCaches
                    .CountAsync(c => c.Aktif && c.SatisFiyati <= 0);
                var cacheZeroStockCount = await _context.MikroProductCaches
                    .CountAsync(c => c.Aktif && c.DepoMiktari <= 0 && c.SatilabilirMiktar <= 0);

                // 3. Product tablosu özeti
                var productTotal = await _context.Products.CountAsync();
                var productWithSku = await _context.Products
                    .CountAsync(p => !string.IsNullOrEmpty(p.SKU));
                var productZeroPrice = await _context.Products
                    .CountAsync(p => p.Price <= 0);
                var productZeroStock = await _context.Products
                    .CountAsync(p => p.StockQuantity <= 0);

                // 4. Cache↔Product tutarsızlık tespiti
                // Aktif cache'de fiyatı olan ama Product'ta hâlâ 0 olan ürün sayısı
                // NEDEN: Bu durum sync'in çalışmadığını gösterir
                var cacheSkusWithPrice = await _context.MikroProductCaches
                    .AsNoTracking()
                    .Where(c => c.Aktif && c.SatisFiyati > 0)
                    .Select(c => c.StokKod)
                    .ToListAsync();

                var mismatchCount = await _context.Products
                    .Where(p => !string.IsNullOrEmpty(p.SKU)
                                && cacheSkusWithPrice.Contains(p.SKU)
                                && p.Price <= 0)
                    .CountAsync();

                sw.Stop();

                _logger.LogInformation(
                    "[MikroDiagnostics] Status sorgulandı. " +
                    "Cache: {CacheTotal}, Product: {ProductTotal}, Uyumsuz: {Mismatch}",
                    cacheTotal, productTotal, mismatchCount);

                return Ok(new
                {
                    generatedAt = DateTime.UtcNow,
                    queryDurationMs = sw.ElapsedMilliseconds,

                    cache = new
                    {
                        total = cacheTotal,
                        active = cacheActive,
                        inactive = cacheInactive,
                        unifiedSource = cacheUnifiedSource,  // SQL_UNIFIED
                        legacySource = cacheLegacySource,    // eski akış
                        zeroPriceCount = cacheZeroPriceCount,
                        zeroStockCount = cacheZeroStockCount,
                        lastSyncTime = cacheStats.LastSyncTime,
                        newestRecord = cacheStats.NewestRecord,
                        oldestRecord = cacheStats.OldestRecord
                    },

                    product = new
                    {
                        total = productTotal,
                        withSku = productWithSku,
                        zeroPriceCount = productZeroPrice,
                        zeroStockCount = productZeroStock
                    },

                    consistency = new
                    {
                        // Cache'de fiyat var ama Product'ta 0 olan ürün sayısı — sync sorunu göstergesi
                        priceMismatchCount = mismatchCount,
                        isSyncHealthy = mismatchCount == 0,
                        note = mismatchCount > 0
                            ? $"{mismatchCount} ürün cache'de fiyatlı ama Product tablosunda 0. SyncCacheToProductTableAsync çalıştırın."
                            : "Cache → Product senkronizasyonu sağlıklı."
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MikroDiagnostics] Status sorgusu hatası");
                return StatusCode(500, new { error = "Diagnostics sorgusu başarısız oldu." });
            }
        }

        // ==================== 8.3: MANUEL SYNC TETİKLEME ====================

        /// <summary>
        /// FAZA 8.3 — Manuel Cache → Product tablosu senkronizasyonu.
        /// Sadece cache→product sync adımını çalıştırır (Mikro'ya gitmez).
        ///
        /// FAYDA: Hangfire job beklemeden canlı sistemde senkronizasyon durumunu test et.
        ///
        /// POST /api/admin/mikro-diagnostics/sync-product-table
        /// </summary>
        [HttpPost("sync-product-table")]
        public async Task<IActionResult> SyncProductTable()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                _logger.LogInformation("[MikroDiagnostics] Manuel SyncCacheToProductTableAsync başlatıldı. Kullanıcı: {User}",
                    User.Identity?.Name ?? "Bilinmiyor");

                var updated = await _cacheService.SyncCacheToProductTableAsync();

                sw.Stop();

                return Ok(new
                {
                    success = true,
                    updatedProductCount = updated,
                    durationMs = sw.ElapsedMilliseconds,
                    message = updated > 0
                        ? $"{updated} ürünün fiyat/stok bilgisi Product tablosuna yansıtıldı."
                        : "Güncellenecek ürün yok (cache ve Product tablosu zaten senkron)."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MikroDiagnostics] Manuel SyncCacheToProductTableAsync hatası");
                return StatusCode(500, new { error = "Senkronizasyon başarısız: " + ex.Message });
            }
        }

        // ==================== 8.4: PERFORMANS BİLGİSİ ====================

        /// <summary>
        /// FAZA 8.4 — Performans karşılaştırma verisi.
        /// Mevcut cache boyutu ve tahmini sync süresi bilgisi döndürür.
        ///
        /// Bu endpoint canlı sistemde performans baselineı oluşturmak için kullanılır.
        ///
        /// GET /api/admin/mikro-diagnostics/performance
        /// </summary>
        [HttpGet("performance")]
        public async Task<IActionResult> GetPerformanceInfo()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                // Cache boyutu ve kaynak dağılımı
                var cacheBySource = await _context.MikroProductCaches
                    .AsNoTracking()
                    .GroupBy(c => c.VeriKaynagi ?? "UNKNOWN")
                    .Select(g => new { source = g.Key, count = g.Count() })
                    .ToListAsync();

                // Fiyat istatistikleri — veri kalitesi kontrolü
                var priceStats = await _context.MikroProductCaches
                    .AsNoTracking()
                    .Where(c => c.Aktif)
                    .GroupBy(c => 1)
                    .Select(g => new
                    {
                        minPrice = g.Min(c => c.SatisFiyati),
                        maxPrice = g.Max(c => c.SatisFiyati),
                        avgPrice = g.Average(c => c.SatisFiyati),
                        zeroPriceCount = g.Count(c => c.SatisFiyati <= 0)
                    })
                    .FirstOrDefaultAsync();

                // Stock istatistikleri
                var stockStats = await _context.MikroProductCaches
                    .AsNoTracking()
                    .Where(c => c.Aktif)
                    .GroupBy(c => 1)
                    .Select(g => new
                    {
                        maxStock = g.Max(c => c.DepoMiktari),
                        avgStock = g.Average(c => c.DepoMiktari),
                        zeroStockCount = g.Count(c => c.DepoMiktari <= 0)
                    })
                    .FirstOrDefaultAsync();

                // Son hareket tarihine göre dağılım — veri tazeliği kontrolü
                var recentCount = await _context.MikroProductCaches
                    .AsNoTracking()
                    .Where(c => c.GuncellemeTarihi >= DateTime.UtcNow.AddHours(-1))
                    .CountAsync();

                sw.Stop();

                return Ok(new
                {
                    generatedAt = DateTime.UtcNow,
                    queryDurationMs = sw.ElapsedMilliseconds,

                    performanceBaseline = new
                    {
                        // FAZA 8.4 tablosu için veri
                        cacheRowCount = cacheBySource.Sum(c => c.count),
                        cacheBySource,
                        priceStats,
                        stockStats,
                        syncedLastHour = recentCount,

                        // İnsan dostu yorum
                        healthSummary = new
                        {
                            isUnifiedMigrationComplete = cacheBySource.Any(c => c.source == "SQL_UNIFIED"),
                            hasLegacyData = cacheBySource.Any(c => c.source != "SQL_UNIFIED"),
                            recommendation = cacheBySource.Any(c => c.source != "SQL_UNIFIED")
                                ? "Bazı kayıtlar eski akıştan geliyor. Full sync tetikleyin (POST /sync-product-table)."
                                : "Tüm kayıtlar SQL_UNIFIED kaynağından. Sistem sağlıklı."
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MikroDiagnostics] Performance bilgisi hatası");
                return StatusCode(500, new { error = "Performans verisi alınamadı." });
            }
        }

        // ==================== 8.1: 0 FİYATLI ÜRÜN LİSTESİ ====================

        /// <summary>
        /// FAZA 8.1 — Fiyat = 0 olan cache kayıtlarını listeler.
        /// Bu ürünlerin neden 0 fiyatlı olduğunu analiz etmek için kullanılır.
        ///
        /// GET /api/admin/mikro-diagnostics/zero-price-products
        /// </summary>
        [HttpGet("zero-price-products")]
        public async Task<IActionResult> GetZeroPriceProducts([FromQuery] int limit = 50)
        {
            // Güvenlik: limit aşırı büyük olamaz
            limit = Math.Clamp(limit, 1, 500);

            try
            {
                var items = await _context.MikroProductCaches
                    .AsNoTracking()
                    .Where(c => c.Aktif && c.SatisFiyati <= 0)
                    .OrderBy(c => c.StokKod)
                    .Take(limit)
                    .Select(c => new
                    {
                        stokKod = c.StokKod,
                        stokAd = c.StokAd,
                        satisFiyati = c.SatisFiyati,
                        depoMiktari = c.DepoMiktari,
                        grupKod = c.GrupKod,
                        veriKaynagi = c.VeriKaynagi,
                        guncellemeTarihi = c.GuncellemeTarihi
                    })
                    .ToListAsync();

                return Ok(new
                {
                    count = items.Count,
                    note = "Bu ürünlerin Mikro'daki sfiyat_fiyati alanını kontrol edin.",
                    items
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MikroDiagnostics] Zero price products sorgusu hatası");
                return StatusCode(500, new { error = "Sorgu başarısız." });
            }
        }
    }
}
