using Microsoft.AspNetCore.Authorization;
using ECommerce.Core.Constants;
using Microsoft.AspNetCore.Mvc;
using ECommerce.Business.Services.Managers;
using ECommerce.Core.Interfaces;
using ECommerce.Core.Interfaces.Cache;
using ECommerce.Core.DTOs.Micro;
using ECommerce.Entities.Concrete;
using ECommerce.Infrastructure.Services.MicroServices;
using ECommerce.API.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using ClosedXML.Excel;
using ECommerce.Core.Interfaces.Sync;
using ECommerce.Core.Interfaces.Mapping;
using ECommerce.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.API.Controllers.Admin
{
    // Type alias ile isim çakışmasını çözüyoruz
    using MikroApiService = ECommerce.Infrastructure.Services.MicroServices.MicroService;

    // Sadece yönetici rollerinin erişimine izin verir
    [Authorize(Roles = Roles.AdminLike)]
    [ApiController]
    // Rota: api/admin/micro
    [Route("api/admin/micro")] 
    public class AdminMicroController : ControllerBase
    {
        private readonly MicroSyncManager _microSyncManager;
        private readonly IMicroService _microService;
        private readonly MikroApiService _mikroApiService;
        private readonly IProductRepository _productRepository;
        private readonly IMikroProductCacheService _cacheService;
        private readonly ISyncLogger _syncLogger;
        private readonly IMikroSyncRepository _syncRepository;
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<AdminMicroController> _logger;
        private readonly ECommerceDbContext _context;
        private const long MaxImportFileSizeBytes = 5 * 1024 * 1024; // 5 MB
        private const int MaxImportRowCount = 10000;
        private static readonly TimeSpan AdminReadCacheTtl = TimeSpan.FromSeconds(10);

        public AdminMicroController(
            MicroSyncManager microSyncManager, 
            IMicroService microService,
            MikroApiService mikroApiService,
            IProductRepository productRepository,
            IMikroProductCacheService cacheService,
            ISyncLogger syncLogger,
            IMikroSyncRepository syncRepository,
            IMemoryCache memoryCache,
            ILogger<AdminMicroController> logger,
            ECommerceDbContext context)
        {
            _microSyncManager = microSyncManager;
            _microService = microService;
            _mikroApiService = mikroApiService;
            _productRepository = productRepository;
            _cacheService = cacheService;
            _syncLogger = syncLogger;
            _syncRepository = syncRepository;
            _memoryCache = memoryCache;
            _logger = logger;
            _context = context;
        }

        //--- Yönetici Yetkisi Gerektiren İşlemler (Mutating/Triggering) ---
        
        /// <summary>
        /// Mikro ERP'den tüm ürünleri çekip yerel veritabanına senkronize et.
        /// Frontend /api/products endpoint'i DB'den okuduğu için arayüze yansıma bu endpoint ile sağlanır.
        /// </summary>
        [HttpPost("sync-products")]
        public async Task<IActionResult> SyncProducts()
        {
            var syncResult = await _microSyncManager.SyncProductsFromMikroAsync();

            return Ok(new
            {
                message = "Mikro ürünleri yerel veritabanına senkronize edildi.",
                totalProducts = syncResult.TotalProducts,
                syncedProducts = syncResult.SyncedProducts,
                createdProducts = syncResult.CreatedProducts,
                updatedProducts = syncResult.UpdatedProducts,
                skippedProducts = syncResult.SkippedProducts,
                failedProducts = syncResult.FailedProducts
            });
        }
        
        /// <summary>
        /// Siparişleri Mikro ERP’ye gönder (Yönetici yetkisi gereklidir)
        /// </summary>
        [HttpPost("export-orders")]
        public async Task<IActionResult> ExportOrders([FromBody] IEnumerable<Order> orders)
        {
            // Bu kritik işlem sadece AdminController altında olmalı
            var success = await _microService.ExportOrdersToERPAsync(orders); 
            
            if (!success)
                return BadRequest(new { message = "Siparişler ERP'ye aktarılamadı." });

            return Ok(new { message = "Siparişler ERP'ye aktarıldı." });
        }
        
        //--- Yönetici Sayfasında Görüntüleme Amaçlı Endpoint'ler (Opsiyonel) ---
        // Eğer bu verileri sadece adminler görüyorsa buraya taşınabilirler. 
        // Ancak herkesin görmesi gerekiyorsa MicroController'da kalmalıdırlar.
        
        /// <summary>
        /// Mikro ERP’den ürünleri getir (Admin sayfasında gösterim için)
        /// </summary>
        [HttpGet("products")]
        public async Task<IActionResult> GetProducts(
            [FromQuery] string source = "db",
            [FromQuery] int page = 1,
            [FromQuery] int perPage = 100)
        {
            page = Math.Max(1, page);
            perPage = Math.Clamp(perPage, 1, 500);

            var sqlData = await _mikroApiService.GetProductsWithSqlAsync(
                depoNo: null,
                fiyatListesiNo: null,
                stokKod: null,
                grupKod: null,
                sadeceStoklu: null,
                sadeceAktif: true,
                sayfaNo: page,
                sayfaBuyuklugu: perPage);

            return Ok(new
            {
                success = true,
                source = "sql",
                page,
                perPage,
                totalCount = sqlData.Count,
                count = sqlData.Count,
                data = sqlData.Select(s => new
                {
                    sku = s.StokKod,
                    name = s.UrunAdi,
                    price = s.Fiyat,
                    stockQuantity = (int)s.StokMiktar,
                    isActive = s.IsWebActive
                })
            });
        }

        /// <summary>
        /// Mikro ERP’den stokları getir (Admin sayfasında gösterim için)
        /// </summary>
        [HttpGet("stocks")]
        public async Task<IActionResult> GetStocks(
            [FromQuery] string source = "db",
            [FromQuery] int page = 1,
            [FromQuery] int perPage = 100)
        {
            page = Math.Max(1, page);
            perPage = Math.Clamp(perPage, 1, 500);

            var sqlData = await _mikroApiService.GetProductsWithSqlAsync(
                depoNo: null,
                fiyatListesiNo: null,
                stokKod: null,
                grupKod: null,
                sadeceStoklu: null,
                sadeceAktif: true,
                sayfaNo: page,
                sayfaBuyuklugu: perPage);

            return Ok(new
            {
                success = true,
                source = "sql",
                page,
                perPage,
                totalCount = sqlData.Count,
                count = sqlData.Count,
                data = sqlData.Select(s => new
                {
                    sku = s.StokKod,
                    quantity = (int)s.StokMiktar,
                    availableQuantity = (int)s.StokMiktar,
                    reservedQuantity = 0
                })
            });
        }

        /// <summary>
        /// ERP'den stokları çekip yerelde günceller (SKU eşlemesi)
        /// </summary>
        [HttpPost("sync-stocks-from-erp")]
        public async Task<IActionResult> SyncStocksFromErp()
        {
            await _microSyncManager.SyncStocksFromMikroAsync();
            return Ok(new { message = "Stoklar ERP'den alınıp güncellendi" });
        }

        /// <summary>
        /// ERP'den fiyatları çekip yerelde günceller (SKU eşlemesi)
        /// </summary>
        [HttpPost("sync-prices-from-erp")]
        public async Task<IActionResult> SyncPricesFromErp()
        {
            await _microSyncManager.SyncPricesFromMikroAsync();
            return Ok(new { message = "Fiyatlar ERP'den alınıp güncellendi" });
        }

        // ==================== MİKRO API V2 DETAYLI ENDPOINT'LER ====================

        /// <summary>
        /// Mikro API StokListesiV2 endpoint'inden detaylı ürün/stok verileri çeker.
        /// Bu endpoint API'den RAW veriyi döndürür.
        /// 
        /// Fiyat listesi artık opsiyoneldir.
        /// 0 gönderilirse ilk uygun fiyat kullanılır ve tüm listeler veri setine dahil edilir.
        /// </summary>
        /// <param name="sayfa">Sayfa numarası (varsayılan: 1)</param>
        /// <param name="sayfaBuyuklugu">Sayfa büyüklüğü (varsayılan: 50)</param>
        /// <param name="depoNo">Depo numarası (varsayılan: 0 = Tüm depolar)</param>
        /// <param name="fiyatListesiNo">Fiyat listesi numarası (0 = tüm listeler / ilk uygun fiyat)</param>
        /// <param name="stokKod">Stok kodu filtresi (opsiyonel)</param>
        /// <param name="grupKod">Grup kodu filtresi (opsiyonel)</param>
        /// <param name="sadeceAktif">Sadece aktif ürünler (varsayılan: true)</param>
        [HttpGet("stok-listesi")]
        public async Task<IActionResult> GetStokListesiV2(
            [FromQuery] int sayfa = 1,
            [FromQuery] int sayfaBuyuklugu = 20,
            [FromQuery] int depoNo = 1, // Varsayılan: Gölköy Depo No 1
            [FromQuery] int fiyatListesiNo = 0,
            [FromQuery] string? stokKod = null,
            [FromQuery] string? grupKod = null,
            [FromQuery] string? aramaMetni = null,
            [FromQuery] bool sadeceAktif = true,
            [FromQuery] bool? sadeceStoklu = null)
        {
            var cacheKey = string.Empty;

            try
            {
                // İş kuralı: API'den ürün dönerken her zaman web aktif ürünler gelir.
                sadeceAktif = true;
                sayfa = Math.Max(1, sayfa);
                sayfaBuyuklugu = Math.Clamp(sayfaBuyuklugu, 1, 1000);

                // 0 = tüm listeler / ilk uygun fiyat, 1-10 = spesifik liste
                fiyatListesiNo = Math.Clamp(fiyatListesiNo, 0, 10);

                cacheKey = string.Join("|",
                    "admin-micro-stok-listesi",
                    sayfa,
                    sayfaBuyuklugu,
                    depoNo,
                    fiyatListesiNo,
                    (stokKod ?? string.Empty).Trim().ToUpperInvariant(),
                    (grupKod ?? string.Empty).Trim().ToUpperInvariant(),
                    (aramaMetni ?? string.Empty).Trim().ToUpperInvariant(),
                    sadeceAktif,
                    sadeceStoklu?.ToString() ?? "null");

                if (_memoryCache.TryGetValue(cacheKey, out object? cachedStokListesiResponse))
                {
                    return Ok(cachedStokListesiResponse);
                }

                _logger.LogInformation(
                    "[AdminMicroController] SQL tabanlı ürün sorgusu çağrılıyor. Sayfa: {Sayfa}, Büyüklük: {Buyukluk}, Depo: {Depo}, FiyatListesi: {FiyatListesi}, StokKod: {StokKod}, GrupKod: {GrupKod}, SadeceStoklu: {SadeceStoklu}",
                    sayfa, sayfaBuyuklugu, depoNo, fiyatListesiNo, stokKod ?? "-", grupKod ?? "-", sadeceStoklu);

                // YENİ: SQL tabanlı birleşik ürün çekme metodu kullan
                // Bu metod STOK_SATIS_FIYAT_LISTELERI_YONETIM + fn_Stok_Depo_Dagilim + STOKLAR
                // tablolarını birleştirerek tüm bilgileri tek sorguda çeker
                var products = await _mikroApiService.GetProductsWithSqlAsync(
                    depoNo: depoNo > 0 ? depoNo : null,
                    fiyatListesiNo: fiyatListesiNo > 0 ? fiyatListesiNo : null,
                    stokKod: stokKod,
                    grupKod: grupKod,
                    sadeceStoklu: sadeceStoklu,
                    sadeceAktif: sadeceAktif,
                    sayfaNo: sayfa,
                    sayfaBuyuklugu: sayfaBuyuklugu
                );

                if (products == null || products.Count == 0)
                {
                    _logger.LogWarning("[AdminMicroController] SQL sorgusu boş sonuç döndü");
                    var emptyResponse = new
                    {
                        success = true,
                        sayfa = sayfa,
                        sayfaBuyuklugu = sayfaBuyuklugu,
                        toplamKayit = 0,
                        toplamSayfa = 0,
                        kayitSayisi = 0,
                        depoNo = depoNo,
                        fiyatListesiNo = fiyatListesiNo,
                        data = new List<object>()
                    };

                    _memoryCache.Set(cacheKey, emptyResponse, AdminReadCacheTtl);
                    return Ok(emptyResponse);
                }

                // Arama filtresi uygula (stok kodu, ürün adı veya grup kodu içinde)
                if (!string.IsNullOrWhiteSpace(aramaMetni) && aramaMetni.Trim().Length >= 3)
                {
                    var searchTerm = aramaMetni.Trim().ToLowerInvariant();
                    products = products.Where(p =>
                        (p.StokKod?.ToLowerInvariant().Contains(searchTerm) == true) ||
                        (p.UrunAdi?.ToLowerInvariant().Contains(searchTerm) == true) ||
                        (p.GrupKod?.ToLowerInvariant().Contains(searchTerm) == true)
                    ).ToList();
                }

                var toplamKayit = products.FirstOrDefault()?.ToplamKayit ?? products.Count;

                var successResponse = new
                {
                    success = true,
                    sayfa = sayfa,
                    sayfaBuyuklugu = sayfaBuyuklugu,
                    toplamKayit = toplamKayit,
                    toplamSayfa = sayfaBuyuklugu > 0 ? (int)Math.Ceiling((decimal)toplamKayit / sayfaBuyuklugu) : 0,
                    kayitSayisi = products.Count,
                    depoNo = depoNo,
                    fiyatListesiNo = fiyatListesiNo,
                    aramaMetni = aramaMetni,
                    aramaMinKarakter = 3,
                    sadeceStoklu = sadeceStoklu,
                    data = products.Select(p => new
                    {
                        stokKod = p.StokKod,
                        stokAd = p.UrunAdi,
                        barkod = p.Barkod,
                        grupKod = p.GrupKod,
                        birim = p.Birim,
                        kdvOrani = p.KdvOrani,
                        satisFiyati = p.Fiyat,
                        depoMiktari = p.StokMiktar,
                        satilabilirMiktar = p.StokMiktar,
                        depoAdi = p.DepoAdi,
                        depoNo = p.DepoNo,
                        aktif = p.IsWebActive,
                        stokDurumu = p.StokDurumu,
                        fiyatFormatli = p.FiyatFormatli,
                        aciklama = p.UrunAdi
                    })
                };

                _memoryCache.Set(cacheKey, successResponse, AdminReadCacheTtl);
                return Ok(successResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AdminMicroController] SQL tabanlı ürün sorgusu hatası - Veritabanından fallback yapılıyor");

                // === FALLBACK: SQL sorgusu başarısız ise veritabanından ürünleri getir ===
                try
                {
                    var dbProducts = await _productRepository.GetAllAsync();
                    var productList = dbProducts
                        .Where(p => p.IsActive)
                        .ToList();

                    // Sayfalama uygula
                    var pagedProducts = productList
                        .Skip((sayfa - 1) * sayfaBuyuklugu)
                        .Take(sayfaBuyuklugu)
                        .ToList();

                    var fallbackResponse = new
                    {
                        success = true,
                        isOfflineMode = true, // Frontend'e offline mod bilgisi
                        message = "Mikro API bağlantısı kurulamadı. Veritabanından yüklendi.",
                        sayfa = sayfa,
                        sayfaBuyuklugu = sayfaBuyuklugu,
                        toplamKayit = productList.Count,
                        toplamSayfa = sayfaBuyuklugu > 0 ? (int)Math.Ceiling((decimal)productList.Count / sayfaBuyuklugu) : 0,
                        kayitSayisi = pagedProducts.Count,
                        depoNo = depoNo,
                        data = pagedProducts.Select(p => new
                        {
                            stokKod = p.SKU ?? $"PRD-{p.Id}",
                            stokAd = p.Name,
                            barkod = "", // Product entity'de Barcode alanı yok
                            grupKod = p.Category?.Name ?? "Genel",
                            birim = "ADET",
                            kdvOrani = 20, // Veritabanı fallback — KDV bilgisi yok
                            satisFiyati = p.Price,
                            depoMiktari = p.StockQuantity,
                            satilabilirMiktar = p.StockQuantity,
                            aktif = p.IsActive,
                            aciklama = p.Description ?? ""
                        })
                    };

                    _memoryCache.Set(cacheKey, fallbackResponse, AdminReadCacheTtl);
                    return Ok(fallbackResponse);
                }
                catch (Exception dbEx)
                {
                    _logger.LogError(dbEx, "[AdminMicroController] Veritabanı fallback da başarısız");
                    return StatusCode(500, new
                    {
                        success = false,
                        message = "Mikro API ve veritabanı bağlantısı kurulamadı: " + ex.Message
                    });
                }
            }
        }

        /// <summary>
        /// Mikro'daki benzersiz grup kodlarını (kategorileri) listeler.
        /// Frontend'de grup kodu dropdown'ı için kullanılır.
        /// </summary>
        [HttpGet("grup-kodlari")]
        public async Task<IActionResult> GetGrupKodlari()
        {
            try
            {
                const string cacheKey = "admin-micro-grup-kodlari";
                if (_memoryCache.TryGetValue(cacheKey, out object? cachedGrupKodlariResponse))
                {
                    return Ok(cachedGrupKodlariResponse);
                }

                _logger.LogInformation("[AdminMicroController] Grup kodları çekiliyor");

                // Önce cache'den dene (hızlı)
                var cachedProducts = await _cacheService.GetAllAsync();
                if (cachedProducts != null && cachedProducts.Any())
                {
                    var grupKodlari = cachedProducts
                        .Where(p => !string.IsNullOrWhiteSpace(p.GrupKod))
                        .Select(p => p.GrupKod!)
                        .Distinct()
                        .OrderBy(g => g)
                        .ToList();

                    var cachedResponse = new
                    {
                        success = true,
                        source = "cache",
                        data = grupKodlari,
                        count = grupKodlari.Count
                    };

                    _memoryCache.Set(cacheKey, cachedResponse, AdminReadCacheTtl);
                    return Ok(cachedResponse);
                }

                // Cache boşsa Mikro API'den çek
                var products = await _mikroApiService.GetProductsWithSqlAsync(
                    sadeceAktif: true,
                    sayfaNo: 1,
                    sayfaBuyuklugu: 200);

                if (products == null || !products.Any())
                {
                    return Ok(new
                    {
                        success = false,
                        message = "Grup kodları alınamadı",
                        data = new List<string>()
                    });
                }

                var grupKodlariFromApi = products
                    .Where(p => !string.IsNullOrWhiteSpace(p.GrupKod))
                    .Select(p => p.GrupKod)
                    .Distinct()
                    .OrderBy(g => g)
                    .ToList();

                var apiResponse = new
                {
                    success = true,
                    source = "api",
                    data = grupKodlariFromApi,
                    count = grupKodlariFromApi.Count
                };

                _memoryCache.Set(cacheKey, apiResponse, AdminReadCacheTtl);
                return Ok(apiResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AdminMicroController] Grup kodları çekme hatası");
                return Ok(new
                {
                    success = false,
                    message = ex.Message,
                    data = new List<string>()
                });
            }
        }

        /// <summary>
        /// Mikro'da bulunan depo numaralarını dinamik olarak listeler.
        /// Frontend depo dropdown'ı için kullanılır.
        /// </summary>
        [HttpGet("depo-listesi")]
        public async Task<IActionResult> GetDepoListesi()
        {
            try
            {
                const string cacheKey = "admin-micro-depo-listesi";
                if (_memoryCache.TryGetValue(cacheKey, out object? cachedDepoListesiResponse))
                {
                    return Ok(cachedDepoListesiResponse);
                }

                var products = await _mikroApiService.GetProductsWithSqlAsync(
                    sadeceAktif: true,
                    sayfaNo: 1,
                    sayfaBuyuklugu: 200);

                var depolar = products
                    .Where(p => p.DepoNo > 0)
                    .GroupBy(p => p.DepoNo)
                    .OrderBy(g => g.Key)
                    .Select(g => new
                    {
                        depoNo = g.Key,
                        depoAdi = g.Select(x => x.DepoAdi).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? $"Depo {g.Key}"
                    })
                    .ToList();

                // Dropdown'da her zaman "Tüm depolar" seçeneği bulunsun.
                depolar.Insert(0, new { depoNo = 0, depoAdi = "Tüm Depolar" });

                var response = new
                {
                    success = true,
                    data = depolar,
                    count = depolar.Count
                };

                _memoryCache.Set(cacheKey, response, AdminReadCacheTtl);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AdminMicroController] Depo listesi çekme hatası");
                return Ok(new
                {
                    success = false,
                    message = ex.Message,
                    data = new[]
                    {
                        new { depoNo = 0, depoAdi = "Tüm Depolar" }
                    }
                });
            }
        }

        /// <summary>
        /// Mikro API bağlantı testi.
        /// SQL direkt bağlantı ile hızlı doğrulama yapar. StokListesiV2 HTTP API timeout
        /// sorunlarını bypass eder.
        /// </summary>
        [HttpGet("test-connection")]
        public async Task<IActionResult> TestConnection()
        {
            try
            {
                _logger.LogInformation("[AdminMicroController] Mikro bağlantı testi başlatılıyor (SQL direkt)");

                // SQL direkt bağlantı ile test — StokListesiV2 HTTP timeout sorununu bypass eder
                var products = await _mikroApiService.GetProductsWithSqlAsync(
                    sayfaNo: 1,
                    sayfaBuyuklugu: 1,
                    sadeceAktif: true);

                var totalCount = products.FirstOrDefault()?.ToplamKayit ?? products.Count;

                if (totalCount > 0)
                {
                    return Ok(new
                    {
                        success = true,
                        message = "Mikro SQL bağlantısı başarılı!",
                        toplamUrunSayisi = totalCount,
                        cekilenKayitSayisi = products.Count,
                        apiVersion = "SQL_DIRECT",
                        timestamp = DateTime.UtcNow
                    });
                }
                else
                {
                    return Ok(new
                    {
                        success = false,
                        message = "Mikro bağlantısı kuruldu ama ürün bulunamadı.",
                        toplamUrunSayisi = 0,
                        timestamp = DateTime.UtcNow
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AdminMicroController] Bağlantı testi hatası");
                
                // Mikro API offline - veritabanı durumunu kontrol et
                try
                {
                    var dbProductCount = (await _productRepository.GetAllAsync()).Count();
                    return Ok(new
                    {
                        isConnected = false,
                        mikroApiOnline = false,
                        databaseOnline = true,
                        message = "Mikro API offline. Veritabanından çalışılabilir.",
                        veritabaniUrunSayisi = dbProductCount,
                        timestamp = DateTime.UtcNow
                    });
                }
                catch
                {
                    return Ok(new
                    {
                        isConnected = false,
                        mikroApiOnline = false,
                        databaseOnline = false,
                        message = "Mikro API ve veritabanı bağlantısı kurulamadı: " + ex.Message,
                        timestamp = DateTime.UtcNow
                    });
                }
            }
        }

        /// <summary>
        /// Mikro API'den belirli bir ürünün detayını getirir.
        /// </summary>
        /// <param name="stokKod">Stok kodu</param>
        [HttpGet("stok/{stokKod}")]
        public async Task<IActionResult> GetStokDetay(string stokKod)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(stokKod))
                    return BadRequest(new { success = false, message = "Stok kodu gerekli" });

                // Belirli stok kodunu aramak için filtre
                var request = new MikroStokListesiRequestDto
                {
                    StokKod = stokKod,
                    SayfaNo = 1,
                    SayfaBuyuklugu = 1
                };

                var result = await _mikroApiService.GetStokListesiV2Async(request);

                if (!result.Success || result.Data == null || !result.Data.Any())
                {
                    return NotFound(new
                    {
                        success = false,
                        message = $"Stok bulunamadı: {stokKod}"
                    });
                }

                var stok = result.Data.First();

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        stokKod = stok.StoKod,
                        stokAd = stok.StoIsim,
                        kisaAd = stok.StoKisaIsmi,
                        grupKod = stok.GrupKodu,
                        anaGrupKod = stok.StoAnagrupKod,
                        altGrupKod = stok.StoAltgrupKod,
                        markaKod = stok.StoMarkaKod,
                        birim = stok.BirimAdi,
                        ikinciBirim = stok.StoBirim2Ad,
                        birimKatsayi = stok.StoBirim2Katsayi,
                        kdvOrani = stok.KdvOrani,
                        perakendeVergi = stok.StoPerakendeVergi,
                        toptanVergi = stok.StoToptanVergi,
                        barkodlar = stok.Barkodlar?.Select(b => new
                        {
                            barkod = b.BarBarkodNo,
                            carpan = b.BarCarpan,
                            anaBarkod = b.BarAnaBarkod
                        }),
                        satisFiyatlari = stok.SatisFiyatlari?.Select(f => new
                        {
                            fiyatNo = f.SfiyatNo,
                            fiyat = f.SfiyatFiyati,
                            dovizCinsi = f.SfiyatDovizCinsi
                        }),
                        toplamStok = stok.StoMiktar,
                        depoMiktar = stok.DepoMiktar,
                        rezerveMiktar = stok.RezerveMiktar,
                        kullanilabilirMiktar = stok.KullanilabilirMiktar,
                        minStok = stok.StoMinMiktar,
                        maxStok = stok.StoMaxMiktar,
                        ortMaliyet = stok.StoOrtMaliyet,
                        sonAlisFiyati = stok.StoSonAlis,
                        brutAgirlik = stok.StoBrutAgirlik,
                        aktif = stok.Aktif,
                        olusturmaTarihi = stok.StoCreateDate,
                        sonGuncelleme = stok.StoLastupDate
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AdminMicroController] Stok detay hatası. StokKod: {StokKod}", stokKod);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Hata: " + ex.Message
                });
            }
        }

        /// <summary>
        /// Seçilen fiyat listesinden ürün fiyatını çözümler.
        /// 
        /// NEDEN: Mikro ERP'de 10'a kadar farklı fiyat listesi olabilir.
        /// Kullanıcı hangi listeyi seçtiyse o listedeki fiyatı döndürürüz.
        /// 
        /// FALLBACK HİYERARŞİSİ:
        /// 1. Seçilen fiyat listesi numarasındaki fiyat (SfiyatNo == fiyatListesiNo)
        /// 2. Fiyat listesi seçilmemişse ilk fiyat > 0 olan liste (SfiyatFiyati > 0)
        /// 3. İlk fiyat > 0 olan liste (SfiyatFiyati > 0)
        /// 3. Son alış fiyatı (StoSonAlis)
        /// 4. 0 (hiçbir fiyat bulunamazsa)
        /// </summary>
        /// <param name="stok">Mikro stok verisi</param>
        /// <param name="fiyatListesiNo">İstenen fiyat listesi numarası (1-10)</param>
        /// <returns>Çözümlenmiş fiyat değeri</returns>
        private static decimal ResolveProductPrice(MikroStokResponseDto stok, int fiyatListesiNo = 1)
        {
            // Fiyat listesi boşsa fallback'e geç
            if (stok.SatisFiyatlari == null || stok.SatisFiyatlari.Count == 0)
            {
                // Son alış fiyatını dene
                if (stok.StoSonAlis.HasValue && stok.StoSonAlis.Value > 0)
                {
                    return stok.StoSonAlis.Value;
                }

                if (stok.StoOrtMaliyet.HasValue && stok.StoOrtMaliyet.Value > 0)
                {
                    return stok.StoOrtMaliyet.Value;
                }

                var alisFallbackOnly = stok.AlisFiyatlari?
                    .OrderBy(f => f.SfiyatNo)
                    .FirstOrDefault(f => f.SfiyatFiyati > 0);
                if (alisFallbackOnly?.SfiyatFiyati > 0)
                {
                    return alisFallbackOnly.SfiyatFiyati;
                }

                return 0m;
            }

            if (fiyatListesiNo > 0)
            {
                var selectedPrice = stok.SatisFiyatlari
                    .FirstOrDefault(f => f.SfiyatNo == fiyatListesiNo);

                if (selectedPrice != null && selectedPrice.SfiyatFiyati > 0)
                {
                    return selectedPrice.SfiyatFiyati;
                }
            }

            // 1/2. Fallback: İlk fiyatı > 0 olan listeyi al (sıralı)
            var fallbackPrice = stok.SatisFiyatlari
                .OrderBy(f => f.SfiyatNo)
                .FirstOrDefault(f => f.SfiyatFiyati > 0);

            if (fallbackPrice != null && fallbackPrice.SfiyatFiyati > 0)
            {
                return fallbackPrice.SfiyatFiyati;
            }

            // Son çare: Son alış fiyatı
            if (stok.StoSonAlis.HasValue && stok.StoSonAlis.Value > 0)
            {
                return stok.StoSonAlis.Value;
            }

            if (stok.StoOrtMaliyet.HasValue && stok.StoOrtMaliyet.Value > 0)
            {
                return stok.StoOrtMaliyet.Value;
            }

            var alisFallback = stok.AlisFiyatlari?
                .OrderBy(f => f.SfiyatNo)
                .FirstOrDefault(f => f.SfiyatFiyati > 0);
            if (alisFallback?.SfiyatFiyati > 0)
            {
                return alisFallback.SfiyatFiyati;
            }

            return 0m;
        }

        private static decimal ResolveProductPriceWithSqlFallback(
            MikroStokResponseDto stok,
            IReadOnlyDictionary<string, MikroFiyatSatirDto> sqlPriceMap,
            int fiyatListesiNo = 0)
        {
            var sku = stok.StoKod ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(sku) &&
                sqlPriceMap.TryGetValue(sku, out var sqlPrice) &&
                sqlPrice.Fiyat > 0)
            {
                return sqlPrice.Fiyat;
            }

            return ResolveProductPrice(stok, fiyatListesiNo);
        }

        private static string? ResolveBarcodeWithSqlFallback(
            MikroStokResponseDto stok,
            IReadOnlyDictionary<string, MikroFiyatSatirDto> sqlPriceMap)
        {
            if (!string.IsNullOrWhiteSpace(stok.Barkod))
            {
                return stok.Barkod;
            }

            var sku = stok.StoKod ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(sku) &&
                sqlPriceMap.TryGetValue(sku, out var sqlPrice) &&
                !string.IsNullOrWhiteSpace(sqlPrice.Barkod) &&
                !string.Equals(sqlPrice.Barkod, "-BARKODYOK-", StringComparison.OrdinalIgnoreCase))
            {
                return sqlPrice.Barkod;
            }

            return stok.Barkod;
        }

        private static bool ResolveWebActiveWithSqlFallback(
            MikroStokResponseDto stok,
            IReadOnlyDictionary<string, MikroFiyatSatirDto> sqlPriceMap,
            IReadOnlyDictionary<string, bool>? cacheActiveStatusMap = null)
        {
            var sku = stok.StoKod ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(sku) &&
                cacheActiveStatusMap != null &&
                cacheActiveStatusMap.TryGetValue(sku, out var cacheActive))
            {
                return cacheActive;
            }

            if (!string.IsNullOrWhiteSpace(sku) &&
                sqlPriceMap.TryGetValue(sku, out var sqlPrice) &&
                sqlPrice.WebeGonderilecekFl.HasValue)
            {
                return sqlPrice.WebeGonderilecekFl.Value;
            }

            // SQL veri yoksa API aktif bilgisine güven.
            return stok.Aktif ?? true;
        }

        private static int ResolveStockQuantityWithSqlFallback(
            MikroStokResponseDto stok,
            IReadOnlyDictionary<string, decimal> sqlStockMap,
            int depoNo = 0)
        {
            var apiStock = ResolveStockQuantity(stok, depoNo);
            if (apiStock > 0)
            {
                return apiStock;
            }

            var sku = stok.StoKod ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(sku) && sqlStockMap.TryGetValue(sku, out var sqlStock) && sqlStock > 0)
            {
                return (int)Math.Max(0, Math.Floor(sqlStock));
            }

            return apiStock;
        }

        /// <summary>
        /// Belirtilen depodaki stok miktarını çözümler.
        /// 
        /// NEDEN: Depo bazlı stok takibi için doğru deponun stoğu gerekli.
        /// depoNo = 0 ise toplam stok döndürülür.
        /// 
        /// STOK KAYNAKLARI (öncelik sırasına göre):
        /// 1. DepoStoklari listesinden belirli depo (depoNo > 0 ise)
        /// 2. DepoMiktar alanı (API'den direkt gelen)
        /// 3. StoMiktar (toplam stok)
        /// </summary>
        /// <param name="stok">Mikro stok verisi</param>
        /// <param name="depoNo">Depo numarası (0 = tüm depolar)</param>
        /// <returns>Stok miktarı (integer)</returns>
        private static int ResolveStockQuantity(MikroStokResponseDto stok, int depoNo = 0)
        {
            // 1) Belirli depo seçildiyse o depoda satılabilir miktarı öncelikle kullan.
            if (depoNo > 0 && stok.DepoStoklari != null && stok.DepoStoklari.Count > 0)
            {
                var depoStok = stok.DepoStoklari.FirstOrDefault(d => d.DepNo == depoNo);
                if (depoStok != null)
                {
                    var depoMiktar = depoStok.SatilabilirMiktar ?? depoStok.StokMiktar;
                    return (int)Math.Max(0, Math.Floor(depoMiktar));
                }
            }

            // 2) Tüm depo toplamı isteniyorsa DepoStoklari toplamını kullan.
            if (depoNo == 0 && stok.DepoStoklari != null && stok.DepoStoklari.Count > 0)
            {
                var toplam = stok.DepoStoklari.Sum(d => d.SatilabilirMiktar ?? d.StokMiktar);
                if (toplam > 0)
                {
                    return (int)Math.Max(0, Math.Floor(toplam));
                }
            }

            // 3) Satılabilir/Kullanılabilir/DepoMiktar alanlarını sırayla değerlendir.
            if (stok.KullanilabilirMiktar.HasValue && stok.KullanilabilirMiktar.Value > 0)
            {
                return (int)Math.Max(0, Math.Floor(stok.KullanilabilirMiktar.Value));
            }

            if (stok.DepoMiktar.HasValue && stok.DepoMiktar.Value > 0)
            {
                return (int)Math.Max(0, Math.Floor(stok.DepoMiktar.Value));
            }

            // 4) Toplam stok miktarı fallback
            if (stok.StoMiktar > 0)
            {
                return (int)Math.Max(0, Math.Floor(stok.StoMiktar));
            }

            // 5) Son fallback: depo stoklarının ham toplamı
            if (stok.DepoStoklari != null && stok.DepoStoklari.Count > 0)
            {
                var toplam = stok.DepoStoklari.Sum(d => d.StokMiktar);
                return (int)Math.Max(0, Math.Floor(toplam));
            }

            return 0;
        }

        /// <summary>
        /// Belirtilen depodaki satılabilir (kullanılabilir) miktarı çözümler.
        /// 
        /// NEDEN: Rezerve edilmiş stoklar satışa uygun değil.
        /// Satılabilir = Toplam Stok - Rezerve
        /// </summary>
        /// <param name="stok">Mikro stok verisi</param>
        /// <param name="depoNo">Depo numarası (0 = tüm depolar)</param>
        /// <returns>Satılabilir miktar (integer)</returns>
        private static int ResolveAvailableQuantity(MikroStokResponseDto stok, int depoNo = 0)
        {
            // Belirli bir depo seçildiyse
            if (depoNo > 0 && stok.DepoStoklari != null && stok.DepoStoklari.Count > 0)
            {
                var depoStok = stok.DepoStoklari.FirstOrDefault(d => d.DepNo == depoNo);
                if (depoStok != null)
                {
                    // Satılabilir miktar direkt tanımlıysa onu kullan
                    if (depoStok.SatilabilirMiktar.HasValue)
                    {
                        return (int)Math.Max(0, Math.Floor(depoStok.SatilabilirMiktar.Value));
                    }
                    // Yoksa stok - rezerve hesapla
                    var available = depoStok.StokMiktar - (depoStok.RezerveMiktar ?? 0);
                    return (int)Math.Max(0, Math.Floor(available));
                }
            }

            // Kullanılabilir miktar direkt tanımlıysa
            if (stok.KullanilabilirMiktar.HasValue && stok.KullanilabilirMiktar.Value > 0)
            {
                return (int)Math.Floor(stok.KullanilabilirMiktar.Value);
            }

            // Toplam stok - Rezerve
            var totalStock = ResolveStockQuantity(stok, depoNo);
            var reserved = (int)Math.Max(0, stok.RezerveMiktar ?? 0);
            return Math.Max(0, totalStock - reserved);
        }

        // ═══════════════════════════════════════════════════════════════════════════════
        // ÜRÜN CACHE SİSTEMİ - 6000+ ürün için performanslı erişim
        // ═══════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Cache'deki ürünleri sayfalı olarak getirir.
        /// ÇOK HIZLI: Local DB'den okur (milisaniyeler içinde).
        /// 
        /// Kullanım: İlk olarak /cache/sync çağırarak cache'i doldurun,
        /// sonra bu endpoint ile sayfalı görüntüleme yapın.
        /// </summary>
        /// <param name="page">Sayfa numarası (varsayılan: 1)</param>
        /// <param name="pageSize">Sayfa büyüklüğü (varsayılan: 50, max: 500)</param>
        /// <param name="stokKod">Stok kodu filtresi</param>
        /// <param name="grupKod">Grup kodu filtresi</param>
        /// <param name="search">Genel arama (stok kodu, ad, barkod)</param>
        /// <param name="sadeceStoklu">Sadece stoklu ürünler (true/false/null=hepsi)</param>
        /// <param name="sortBy">Sıralama alanı (stokKod, stokAd, satisFiyati, depoMiktari)</param>
        /// <param name="sortDesc">Azalan sıralama</param>
        [HttpGet("cache/products")]
        public async Task<IActionResult> GetCachedProducts(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            [FromQuery] string? stokKod = null,
            [FromQuery] string? grupKod = null,
            [FromQuery] string? search = null,
            [FromQuery] bool? sadeceStoklu = null,
            [FromQuery] bool? sadeceAktif = null,
            [FromQuery] string? sortBy = "stokKod",
            [FromQuery] bool sortDesc = false)
        {
            try
            {
                // API genelinde web aktif ürün gösterim kuralı.
                sadeceAktif = true;

                // Sayfa boyutunu sınırla
                pageSize = Math.Clamp(pageSize, 1, 500);

                var query = new MikroCacheQuery
                {
                    Page = page,
                    PageSize = pageSize,
                    StokKodFilter = stokKod,
                    GrupKodFilter = grupKod,
                    SearchTerm = search,
                    SadeceStokluOlanlar = sadeceStoklu,
                    SadeceAktif = sadeceAktif,
                    SortBy = sortBy,
                    SortDescending = sortDesc
                };

                var result = await _cacheService.GetCachedProductsAsync(query);

                return Ok(new
                {
                    success = true,
                    data = result.Items.Select(p => new
                    {
                        stokKod = p.StokKod,
                        stokAd = p.StokAd,
                        barkod = p.Barkod,
                        grupKod = p.GrupKod,
                        birim = p.Birim,
                        kdvOrani = p.KdvOrani,
                        satisFiyati = p.SatisFiyati,
                        fiyatListesiNo = p.FiyatListesiNo,
                        depoMiktari = p.DepoMiktari,
                        satilabilirMiktar = p.SatilabilirMiktar,
                        depoNo = p.DepoNo,
                        aktif = p.Aktif,
                        localProductId = p.LocalProductId,
                        syncStatus = p.SyncStatus,
                        guncellemeTarihi = p.GuncellemeTarihi
                    }),
                    pagination = new
                    {
                        page = result.Page,
                        pageSize = result.PageSize,
                        totalCount = result.TotalCount,
                        totalPages = result.TotalPages,
                        hasPreviousPage = result.HasPreviousPage,
                        hasNextPage = result.HasNextPage
                    },
                    lastSyncTime = result.LastSyncTime
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AdminMicroController] GetCachedProducts hatası");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Cache'deki TÜM ürünleri CSV export için getirir (limit yok).
        /// CSV indirme işlemi için kullanılır - tüm ürünler tek seferde döner.
        /// </summary>
        /// <param name="grupKod">Grup kodu filtresi (opsiyonel)</param>
        /// <param name="sadeceStoklu">Sadece stoklu ürünler (true/false/null=hepsi)</param>
        /// <param name="sadeceAktif">Sadece aktif ürünler (true/false/null=hepsi)</param>
        [HttpGet("cache/export-all")]
        public async Task<IActionResult> ExportAllCachedProducts(
            [FromQuery] string? grupKod = null,
            [FromQuery] bool? sadeceStoklu = null,
            [FromQuery] bool? sadeceAktif = null)
        {
            try
            {
                // API genelinde export da sadece web aktif ürünleri döndürür.
                sadeceAktif = true;

                // Tüm ürünleri çekmek için büyük pageSize kullan (limit yok)
                var query = new MikroCacheQuery
                {
                    Page = 1,
                    PageSize = 100000, // Yeterince büyük
                    GrupKodFilter = grupKod,
                    SadeceStokluOlanlar = sadeceStoklu,
                    SadeceAktif = sadeceAktif,
                    SortBy = "stokKod",
                    SortDescending = false
                };

                var result = await _cacheService.GetCachedProductsAsync(query);

                _logger.LogInformation(
                    "[AdminMicroController] ExportAllCachedProducts - {Count} ürün export edildi",
                    result.Items.Count);

                return Ok(new
                {
                    success = true,
                    data = result.Items.Select(p => new
                    {
                        stokKod = p.StokKod,
                        stokAd = p.StokAd,
                        barkod = p.Barkod,
                        grupKod = p.GrupKod,
                        birim = p.Birim,
                        kdvOrani = p.KdvOrani,
                        satisFiyati = p.SatisFiyati,
                        fiyatListesiNo = p.FiyatListesiNo,
                        depoMiktari = p.DepoMiktari,
                        satilabilirMiktar = p.SatilabilirMiktar,
                        depoNo = p.DepoNo,
                        aktif = p.Aktif
                    }),
                    totalCount = result.TotalCount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AdminMicroController] ExportAllCachedProducts hatası");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Tek ürünün aktif/pasif durumunu değiştirir.
        /// NEDEN: Admin panelden ürünleri manuel olarak satışa açma/kapatma için.
        /// </summary>
        /// <param name="stokKod">Stok kodu</param>
        /// <param name="aktif">Yeni aktiflik durumu</param>
        [HttpPut("cache/products/{stokKod}/toggle-active")]
        public async Task<IActionResult> ToggleProductActive(
            [FromRoute] string stokKod,
            [FromQuery] bool aktif)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(stokKod))
                {
                    return BadRequest(new { success = false, message = "Stok kodu gerekli" });
                }

                var result = await _cacheService.SetProductActiveStatusAsync(stokKod, aktif);
                
                if (result)
                {
                    _logger.LogInformation(
                        "[AdminMicroController] Ürün aktiflik durumu değiştirildi. StokKod: {StokKod}, Aktif: {Aktif}",
                        stokKod, aktif);

                    return Ok(new
                    {
                        success = true,
                        message = aktif ? "Ürün aktif edildi" : "Ürün pasif edildi",
                        stokKod = stokKod,
                        aktif = aktif
                    });
                }
                else
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Ürün bulunamadı",
                        stokKod = stokKod
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AdminMicroController] ToggleProductActive hatası. StokKod: {StokKod}", stokKod);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Birden fazla ürünün aktif/pasif durumunu toplu değiştirir.
        /// NEDEN: Admin panelden seçili ürünleri topluca aktif/pasif yapma için.
        /// </summary>
        [HttpPut("cache/products/bulk-toggle-active")]
        public async Task<IActionResult> BulkToggleProductActive(
            [FromBody] BulkToggleActiveRequest request)
        {
            try
            {
                if (request?.StokKodlar == null || !request.StokKodlar.Any())
                {
                    return BadRequest(new { success = false, message = "En az bir stok kodu gerekli" });
                }

                var successCount = 0;
                var failedCount = 0;
                var failedCodes = new List<string>();

                foreach (var stokKod in request.StokKodlar)
                {
                    var result = await _cacheService.SetProductActiveStatusAsync(stokKod, request.Aktif);
                    if (result)
                    {
                        successCount++;
                    }
                    else
                    {
                        failedCount++;
                        failedCodes.Add(stokKod);
                    }
                }

                _logger.LogInformation(
                    "[AdminMicroController] Toplu aktiflik değişikliği. Başarılı: {Success}, Başarısız: {Failed}, Aktif: {Aktif}",
                    successCount, failedCount, request.Aktif);

                return Ok(new
                {
                    success = failedCount == 0,
                    message = $"{successCount} ürün {(request.Aktif ? "aktif" : "pasif")} edildi" +
                              (failedCount > 0 ? $", {failedCount} ürün bulunamadı" : ""),
                    stats = new
                    {
                        successCount = successCount,
                        failedCount = failedCount,
                        failedCodes = failedCodes
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AdminMicroController] BulkToggleProductActive hatası");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Excel dosyasından ürün aktiflik durumlarını toplu günceller.
        /// 
        /// EXCEL FORMATI:
        /// - İlk satır: Başlık satırı (StokKod, Aktif)
        /// - StokKod: Zorunlu - Mikro stok kodu
        /// - Aktif: Opsiyonel - true/false, 1/0, Evet/Hayır (yoksa true kabul edilir)
        /// 
        /// KULLANIM:
        /// 1. Önce template'i indirin (/api/admin/micro/import/template)
        /// 2. Stok kodlarını doldurun
        /// 3. Bu endpoint'e POST edin
        /// </summary>
        [HttpPost("import/active-products")]
        public async Task<IActionResult> ImportActiveProducts(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { success = false, message = "Dosya seçilmedi" });
                }

                if (file.Length > MaxImportFileSizeBytes)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = $"Dosya boyutu çok büyük. Maksimum {(MaxImportFileSizeBytes / 1024 / 1024)} MB yükleyebilirsiniz"
                    });
                }

                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (extension != ".xlsx" && extension != ".xls" && extension != ".csv")
                {
                    return BadRequest(new { success = false, message = "Sadece .xlsx, .xls veya .csv dosyaları kabul edilir" });
                }

                _logger.LogInformation(
                    "[AdminMicroController] Excel import başlatılıyor. Dosya: {FileName}, Boyut: {Size}KB",
                    file.FileName, file.Length / 1024);

                var importResults = new List<ExcelImportRow>();
                var successCount = 0;
                var failedCount = 0;
                var skippedCount = 0;
                var activatedCount = 0;
                var deactivatedCount = 0;
                var processedCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                // Excel'i parse et
                using var stream = file.OpenReadStream();
                
                if (extension == ".csv")
                {
                    using var reader = new StreamReader(stream);
                    var headerLine = await reader.ReadLineAsync();
                    if (string.IsNullOrWhiteSpace(headerLine))
                    {
                        return BadRequest(new { success = false, message = "CSV başlık satırı boş" });
                    }

                    var delimiter = DetectDelimiter(headerLine);
                    var headers = SplitDelimitedLine(headerLine, delimiter)
                        .Select(h => NormalizeHeader(h))
                        .ToList();

                    var stokKodIndex = headers.FindIndex(h => h == "stokkod");
                    var aktifIndex = headers.FindIndex(h => h == "aktif");

                    if (stokKodIndex < 0)
                    {
                        return BadRequest(new
                        {
                            success = false,
                            message = "CSV başlığı geçersiz. 'StokKod' kolonu zorunludur"
                        });
                    }

                    var lineNumber = 1;

                    while (!reader.EndOfStream)
                    {
                        lineNumber++;
                        var line = await reader.ReadLineAsync();
                        if (string.IsNullOrWhiteSpace(line)) continue;

                        if (lineNumber > MaxImportRowCount + 1)
                        {
                            importResults.Add(new ExcelImportRow
                            {
                                SatirNo = lineNumber,
                                Success = false,
                                Mesaj = $"Satır limiti aşıldı. En fazla {MaxImportRowCount} satır işlenebilir"
                            });
                            skippedCount++;
                            continue;
                        }

                        var parts = SplitDelimitedLine(line, delimiter);
                        var stokKod = parts.Length > stokKodIndex ? parts[stokKodIndex].Trim().Trim('"') : "";
                        var aktifStr = (aktifIndex >= 0 && parts.Length > aktifIndex)
                            ? parts[aktifIndex].Trim().Trim('"')
                            : "true";

                        var row = ProcessImportRow(stokKod, aktifStr, lineNumber);

                        if (row.Success && !processedCodes.Add(row.StokKod))
                        {
                            row.Success = false;
                            row.Mesaj = "Bu stok kodu dosyada birden fazla kez geçiyor";
                        }

                        importResults.Add(row);

                        if (row.Success)
                        {
                            var result = await _cacheService.SetProductActiveStatusAsync(row.StokKod, row.Aktif);
                            if (result)
                            {
                                successCount++;
                                if (row.Aktif) activatedCount++;
                                else deactivatedCount++;
                            }
                            else
                            {
                                failedCount++;
                                row.Mesaj = "Ürün veritabanında bulunamadı";
                                row.Success = false;
                            }
                        }
                        else
                        {
                            skippedCount++;
                        }
                    }
                }
                else
                {
                    stream.Position = 0;
                    using var workbook = new XLWorkbook(stream);
                    var worksheet = workbook.Worksheets.FirstOrDefault();
                    if (worksheet == null)
                    {
                        return BadRequest(new { success = false, message = "Excel içinde çalışma sayfası bulunamadı" });
                    }

                    var usedRange = worksheet.RangeUsed();
                    if (usedRange == null)
                    {
                        return BadRequest(new { success = false, message = "Excel dosyası boş" });
                    }

                    var headerRow = usedRange.FirstRow();
                    var headerCells = headerRow.Cells().ToList();
                    var headerMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

                    for (int i = 0; i < headerCells.Count; i++)
                    {
                        var normalized = NormalizeHeader(headerCells[i].GetString());
                        if (!string.IsNullOrWhiteSpace(normalized) && !headerMap.ContainsKey(normalized))
                        {
                            headerMap[normalized] = i + 1;
                        }
                    }

                    if (!headerMap.TryGetValue("stokkod", out var stokKodCol))
                    {
                        return BadRequest(new
                        {
                            success = false,
                            message = "Excel başlığı geçersiz. 'StokKod' kolonu zorunludur"
                        });
                    }

                    var aktifCol = headerMap.TryGetValue("aktif", out var activeColIndex)
                        ? activeColIndex
                        : -1;

                    var rowCount = usedRange.RowCount() - 1; // header hariç
                    if (rowCount > MaxImportRowCount)
                    {
                        return BadRequest(new
                        {
                            success = false,
                            message = $"Satır limiti aşıldı. En fazla {MaxImportRowCount} satır yükleyebilirsiniz"
                        });
                    }

                    var dataRows = usedRange.RowsUsed().Skip(1).ToList();
                    foreach (var dataRow in dataRows)
                    {
                        var lineNumber = dataRow.RowNumber();
                        var stokKod = dataRow.Cell(stokKodCol).GetString().Trim();
                        var aktifStr = aktifCol > 0 ? dataRow.Cell(aktifCol).GetString().Trim() : "true";

                        var row = ProcessImportRow(stokKod, aktifStr, lineNumber);

                        if (row.Success && !processedCodes.Add(row.StokKod))
                        {
                            row.Success = false;
                            row.Mesaj = "Bu stok kodu dosyada birden fazla kez geçiyor";
                        }

                        importResults.Add(row);

                        if (row.Success)
                        {
                            var result = await _cacheService.SetProductActiveStatusAsync(row.StokKod, row.Aktif);
                            if (result)
                            {
                                successCount++;
                                if (row.Aktif) activatedCount++;
                                else deactivatedCount++;
                            }
                            else
                            {
                                failedCount++;
                                row.Mesaj = "Ürün veritabanında bulunamadı";
                                row.Success = false;
                            }
                        }
                        else
                        {
                            skippedCount++;
                        }
                    }
                }

                _logger.LogInformation(
                    "[AdminMicroController] Excel import tamamlandı. Başarılı: {Success}, Başarısız: {Failed}, Atlanan: {Skipped}",
                    successCount, failedCount, skippedCount);

                return Ok(new
                {
                    success = failedCount == 0,
                    message = $"{successCount} ürün işlendi (Aktif: {activatedCount}, Pasif: {deactivatedCount})" +
                              (failedCount > 0 ? $", {failedCount} ürün başarısız" : "") +
                              (skippedCount > 0 ? $", {skippedCount} satır atlandı" : ""),
                    stats = new
                    {
                        totalRows = importResults.Count,
                        successCount,
                        failedCount,
                        skippedCount,
                        activatedCount,
                        deactivatedCount
                    },
                    details = importResults.Where(r => !r.Success).Take(100).ToList() // Hatalı satırları göster
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AdminMicroController] Excel import hatası");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Excel import için template dosyası indirir.
        /// </summary>
        [HttpGet("import/template")]
        public IActionResult DownloadImportTemplate()
        {
            var csvContent = "StokKod,Aktif\nORNEK001,true\nORNEK002,false\n";
            var bytes = System.Text.Encoding.UTF8.GetBytes(csvContent);
            return File(bytes, "text/csv", "mikro_aktif_urunler_template.csv");
        }

        /// <summary>
        /// Import satırını işler ve doğrular.
        /// </summary>
        private ExcelImportRow ProcessImportRow(string stokKod, string aktifStr, int lineNumber)
        {
            var row = new ExcelImportRow
            {
                SatirNo = lineNumber,
                StokKod = (stokKod ?? "").Trim().ToUpperInvariant()
            };

            // StokKod kontrolü
            if (string.IsNullOrWhiteSpace(row.StokKod))
            {
                row.Success = false;
                row.Mesaj = "Stok kodu boş";
                return row;
            }

            if (row.StokKod.Length > 50)
            {
                row.Success = false;
                row.Mesaj = "Stok kodu çok uzun (max 50 karakter)";
                return row;
            }

            // Aktif değerini parse et
            var aktifLower = (aktifStr ?? "true").Trim().ToLowerInvariant();
            var knownTrue = new[] { "true", "1", "evet", "yes", "aktif", "e" };
            var knownFalse = new[] { "false", "0", "hayır", "hayir", "no", "pasif", "h" };

            if (string.IsNullOrWhiteSpace(aktifLower))
            {
                row.Aktif = true;
            }
            else if (knownTrue.Contains(aktifLower))
            {
                row.Aktif = true;
            }
            else if (knownFalse.Contains(aktifLower))
            {
                row.Aktif = false;
            }
            else
            {
                row.Success = false;
                row.Mesaj = "Aktif kolonu geçersiz (true/false, 1/0, evet/hayır)";
                return row;
            }

            row.Success = true;
            row.Mesaj = row.Aktif ? "Aktif edilecek" : "Pasif edilecek";
            return row;
        }

        private static string NormalizeHeader(string? header)
        {
            if (string.IsNullOrWhiteSpace(header)) return string.Empty;
            return header.Trim().ToLowerInvariant().Replace(" ", string.Empty).Replace("_", string.Empty);
        }

        private static char DetectDelimiter(string line)
        {
            var semicolon = line.Count(c => c == ';');
            var comma = line.Count(c => c == ',');
            var tab = line.Count(c => c == '\t');

            if (tab >= semicolon && tab >= comma) return '\t';
            if (semicolon >= comma) return ';';
            return ',';
        }

        private static string[] SplitDelimitedLine(string line, char delimiter)
        {
            if (line == null) return Array.Empty<string>();

            var values = new List<string>();
            var current = new System.Text.StringBuilder();
            var inQuotes = false;

            for (var i = 0; i < line.Length; i++)
            {
                var ch = line[i];

                if (ch == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }

                    continue;
                }

                if (ch == delimiter && !inQuotes)
                {
                    values.Add(current.ToString());
                    current.Clear();
                    continue;
                }

                current.Append(ch);
            }

            values.Add(current.ToString());
            return values.ToArray();
        }

        // ==================== MİKRO'YA YAZMA (ToERP) ====================

        /// <summary>
        /// Ürün bilgilerini Mikro ERP'ye günceller.
        /// İsim, açıklama, KDV oranı, birim gibi temel bilgileri senkronize eder.
        /// </summary>
        /// <param name="request">Güncellenecek ürün bilgileri</param>
        [HttpPut("sync/product")]
        public async Task<IActionResult> SyncProductToMikro([FromBody] MikroProductUpdateRequest request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest(new { success = false, message = "İstek gövdesi boş" });
                }

                request.StokKod = (request.StokKod ?? string.Empty).Trim().ToUpperInvariant();

                if (string.IsNullOrWhiteSpace(request.StokKod))
                {
                    return BadRequest(new { success = false, message = "Stok kodu gerekli" });
                }

                if (request.KdvOrani.HasValue && (request.KdvOrani < 0 || request.KdvOrani > 100))
                {
                    return BadRequest(new { success = false, message = "KDV oranı 0 ile 100 arasında olmalı" });
                }

                if (!string.IsNullOrWhiteSpace(request.Birim) && request.Birim!.Trim().Length > 10)
                {
                    return BadRequest(new { success = false, message = "Birim en fazla 10 karakter olabilir" });
                }

                var hasAnyProductField =
                    !string.IsNullOrWhiteSpace(request.StokAd) ||
                    !string.IsNullOrWhiteSpace(request.Barkod) ||
                    !string.IsNullOrWhiteSpace(request.Birim) ||
                    request.KdvOrani.HasValue ||
                    !string.IsNullOrWhiteSpace(request.GrupKod) ||
                    !string.IsNullOrWhiteSpace(request.Aciklama) ||
                    !string.IsNullOrWhiteSpace(request.ResimUrl);

                if (!hasAnyProductField)
                {
                    return BadRequest(new { success = false, message = "Güncellenecek en az bir alan gönderilmelidir" });
                }

                _logger.LogInformation(
                    "[AdminMicroController] Ürün Mikro'ya sync ediliyor. StokKod: {StokKod}",
                    request.StokKod);

                // MikroStokKaydetRequestDto oluştur
                var mikroRequest = new ECommerce.Core.DTOs.Micro.MikroStokKaydetRequestDto
                {
                    StoKod = request.StokKod,
                    StoIsim = request.StokAd ?? "",
                    StoBirim1Ad = request.Birim ?? "ADET",
                    StoToptanVergi = request.KdvOrani ?? 20,
                    StoPerakendeVergi = request.KdvOrani ?? 20,
                    StoKisaIsmi = request.Aciklama
                };

                // Grup kodu varsa ekle
                if (!string.IsNullOrWhiteSpace(request.GrupKod))
                {
                    mikroRequest.StoAnagrupKod = request.GrupKod;
                }

                // Barkod varsa ekle
                if (!string.IsNullOrWhiteSpace(request.Barkod))
                {
                    mikroRequest.Barkodlar.Add(new ECommerce.Core.DTOs.Micro.MikroStokBarkodDto
                    {
                        BarBarkodNo = request.Barkod,
                        BarCarpan = 1,
                        BarBirimi = 0
                    });
                }

                // Resim URL varsa ekle
                if (!string.IsNullOrWhiteSpace(request.ResimUrl))
                {
                    mikroRequest.Resimler = new List<ECommerce.Core.DTOs.Micro.MikroStokResimDto>
                    {
                        new ECommerce.Core.DTOs.Micro.MikroStokResimDto
                        {
                            ResimUrl = request.ResimUrl,
                            ResimSira = 1
                        }
                    };
                }

                // Mikro'ya gönder
                var result = await _mikroApiService.SaveStokV2Async(mikroRequest);

                if (result.Success)
                {
                    // Local cache'i de güncelle
                    // TODO: Cache update logic burada eklenebilir

                    _logger.LogInformation(
                        "[AdminMicroController] Ürün Mikro'ya sync edildi. StokKod: {StokKod}",
                        request.StokKod);

                    return Ok(new
                    {
                        success = true,
                        message = $"Ürün {request.StokKod} Mikro'ya senkronize edildi",
                        stokKod = request.StokKod
                    });
                }
                else
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = result.Message ?? "Mikro sync başarısız",
                        stokKod = request.StokKod
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AdminMicroController] Ürün sync hatası. StokKod: {StokKod}", request.StokKod);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Ürün fiyatını Mikro ERP'ye günceller.
        /// Belirtilen fiyat listesine yeni fiyatı yazar.
        /// </summary>
        /// <param name="request">Fiyat güncelleme bilgileri</param>
        [HttpPut("sync/price")]
        public async Task<IActionResult> SyncPriceToMikro([FromBody] MikroPriceUpdateRequest request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest(new { success = false, message = "İstek gövdesi boş" });
                }

                request.StokKod = (request.StokKod ?? string.Empty).Trim().ToUpperInvariant();

                if (string.IsNullOrWhiteSpace(request.StokKod))
                {
                    return BadRequest(new { success = false, message = "Stok kodu gerekli" });
                }

                if (request.YeniFiyat <= 0)
                {
                    return BadRequest(new { success = false, message = "Geçerli bir fiyat girilmeli" });
                }

                if (request.FiyatListesiNo.HasValue && (request.FiyatListesiNo < 1 || request.FiyatListesiNo > 10))
                {
                    return BadRequest(new { success = false, message = "Fiyat listesi 1 ile 10 arasında olmalı" });
                }

                _logger.LogInformation(
                    "[AdminMicroController] Fiyat Mikro'ya sync ediliyor. StokKod: {StokKod}, YeniFiyat: {Fiyat}",
                    request.StokKod, request.YeniFiyat);

                // MikroFiyatDegisikligiRequestDto oluştur
                var mikroRequest = new ECommerce.Core.DTOs.Micro.MikroFiyatDegisikligiRequestDto
                {
                    StoKod = request.StokKod,
                    YeniFiyat = request.YeniFiyat,
                    FiyatNo = request.FiyatListesiNo ?? 1,
                    KdvDahil = request.KdvDahil ?? true
                };

                // Mikro'ya gönder
                var result = await _mikroApiService.SaveFiyatDegisikligiV2Async(mikroRequest);

                if (result.Success)
                {
                    _logger.LogInformation(
                        "[AdminMicroController] Fiyat Mikro'ya sync edildi. StokKod: {StokKod}",
                        request.StokKod);

                    return Ok(new
                    {
                        success = true,
                        message = $"Ürün {request.StokKod} fiyatı Mikro'ya senkronize edildi",
                        stokKod = request.StokKod,
                        yeniFiyat = request.YeniFiyat
                    });
                }
                else
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = result.Message ?? "Fiyat sync başarısız",
                        stokKod = request.StokKod
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AdminMicroController] Fiyat sync hatası. StokKod: {StokKod}", request.StokKod);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Ürün bilgilerini ve fiyatı birlikte Mikro ERP'ye günceller.
        /// İsim + Fiyat değişikliği için tek istekte senkronizasyon yapar.
        /// </summary>
        /// <param name="request">Ürün ve fiyat bilgileri</param>
        [HttpPut("sync/product-full")]
        public async Task<IActionResult> SyncProductFullToMikro([FromBody] MikroFullUpdateRequest request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest(new { success = false, message = "İstek gövdesi boş" });
                }

                request.StokKod = (request.StokKod ?? string.Empty).Trim().ToUpperInvariant();

                if (string.IsNullOrWhiteSpace(request.StokKod))
                {
                    return BadRequest(new { success = false, message = "Stok kodu gerekli" });
                }

                var hasProductPart =
                    !string.IsNullOrWhiteSpace(request.StokAd) ||
                    !string.IsNullOrWhiteSpace(request.Barkod) ||
                    !string.IsNullOrWhiteSpace(request.Birim) ||
                    request.KdvOrani.HasValue ||
                    !string.IsNullOrWhiteSpace(request.GrupKod) ||
                    !string.IsNullOrWhiteSpace(request.Aciklama) ||
                    !string.IsNullOrWhiteSpace(request.ResimUrl);
                var hasPricePart = request.SatisFiyati.HasValue;

                if (!hasProductPart && !hasPricePart)
                {
                    return BadRequest(new { success = false, message = "Güncellenecek alan gönderilmedi" });
                }

                if (request.KdvOrani.HasValue && (request.KdvOrani < 0 || request.KdvOrani > 100))
                {
                    return BadRequest(new { success = false, message = "KDV oranı 0 ile 100 arasında olmalı" });
                }

                if (request.SatisFiyati.HasValue && request.SatisFiyati <= 0)
                {
                    return BadRequest(new { success = false, message = "Fiyat 0'dan büyük olmalı" });
                }

                if (request.FiyatListesiNo.HasValue && (request.FiyatListesiNo < 1 || request.FiyatListesiNo > 10))
                {
                    return BadRequest(new { success = false, message = "Fiyat listesi 1 ile 10 arasında olmalı" });
                }

                _logger.LogInformation(
                    "[AdminMicroController] Tam ürün sync başlatılıyor. StokKod: {StokKod}",
                    request.StokKod);

                var errors = new List<string>();
                var successOps = new List<string>();

                // 1. Ürün bilgileri güncelle (isim, açıklama vb. varsa)
                if (!string.IsNullOrWhiteSpace(request.StokAd) || !string.IsNullOrWhiteSpace(request.Barkod))
                {
                    var productRequest = new MikroProductUpdateRequest
                    {
                        StokKod = request.StokKod,
                        StokAd = request.StokAd,
                        Barkod = request.Barkod,
                        Birim = request.Birim,
                        KdvOrani = request.KdvOrani,
                        GrupKod = request.GrupKod,
                        Aciklama = request.Aciklama,
                        ResimUrl = request.ResimUrl
                    };

                    var produktResult = await SyncProductToMikro(productRequest);
                    if (produktResult is OkObjectResult)
                    {
                        successOps.Add("Ürün bilgileri");
                    }
                    else
                    {
                        errors.Add("Ürün bilgileri güncellenemedi");
                    }
                }

                // 2. Fiyat güncelle (fiyat değeri varsa)
                if (request.SatisFiyati.HasValue && request.SatisFiyati > 0)
                {
                    var priceRequest = new MikroPriceUpdateRequest
                    {
                        StokKod = request.StokKod,
                        YeniFiyat = request.SatisFiyati.Value,
                        FiyatListesiNo = request.FiyatListesiNo ?? 1,
                        KdvDahil = request.KdvDahil ?? true
                    };

                    var priceResult = await SyncPriceToMikro(priceRequest);
                    if (priceResult is OkObjectResult)
                    {
                        successOps.Add("Fiyat");
                    }
                    else
                    {
                        errors.Add("Fiyat güncellenemedi");
                    }
                }

                // Sonuç
                if (errors.Count == 0)
                {
                    return Ok(new
                    {
                        success = true,
                        message = $"Ürün {request.StokKod} tam senkronize edildi: {string.Join(", ", successOps)}",
                        stokKod = request.StokKod
                    });
                }
                else if (successOps.Count > 0)
                {
                    return Ok(new
                    {
                        success = true,
                        message = $"Kısmi başarılı: {string.Join(", ", successOps)} güncellendi. Hatalar: {string.Join(", ", errors)}",
                        stokKod = request.StokKod,
                        warnings = errors
                    });
                }
                else
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = $"Senkronizasyon başarısız: {string.Join(", ", errors)}",
                        stokKod = request.StokKod
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AdminMicroController] Tam ürün sync hatası. StokKod: {StokKod}", request.StokKod);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Tüm ürünleri Mikro ERP'den çeker ve cache'e kaydeder.
        /// 
        /// İLK KULLANIM: Bu endpoint'i bir kez çağırın, tüm ürünler local DB'ye kaydedilir.
        /// SONRAKI KULLANIM: /cache/products endpoint'i ile sayfalı görüntüleme yapın.
        /// 
        /// 6000 ürün için yaklaşık 1-2 dakika sürer (batch + throttle ile).
        /// </summary>
        /// <param name="fiyatListesiNo">Fiyat listesi numarası (1-10)</param>
        /// <param name="depoNo">Depo numarası (0 = tüm depolar)</param>
        [HttpPost("cache/sync")]
        public async Task<IActionResult> SyncProductCache(
            [FromQuery] int fiyatListesiNo = 1,
            [FromQuery] int depoNo = 0,
            [FromQuery] string syncMode = "newOnly")
        {
            try
            {
                _logger.LogInformation(
                    "[AdminMicroController] Cache sync başlatılıyor. FiyatListesi: {FiyatListesi}, Depo: {Depo}, Mod: {Mode}",
                    fiyatListesiNo, depoNo, syncMode);

                var normalizedMode = (syncMode ?? "newOnly").Trim().ToLowerInvariant();
                var result = normalizedMode switch
                {
                    "full" => await _cacheService.FetchAllAndCacheAsync(fiyatListesiNo, depoNo),
                    "newonly" => await _cacheService.SyncNewProductsOnlyAsync(fiyatListesiNo, depoNo),
                    _ => await _cacheService.SyncNewProductsOnlyAsync(fiyatListesiNo, depoNo)
                };

                if (result.Success)
                {
                    return Ok(new
                    {
                        success = true,
                        message = result.Message,
                        stats = new
                        {
                            totalFetched = result.TotalFetched,
                            newProducts = result.NewProducts,
                            updatedProducts = result.UpdatedProducts,
                            unchangedProducts = result.UnchangedProducts,
                            durationSeconds = result.Duration.TotalSeconds
                        }
                    });
                }
                else
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = result.Message,
                        errors = result.Errors
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AdminMicroController] SyncProductCache hatası");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Cache istatistiklerini getirir.
        /// Toplam ürün sayısı, stoklu/stoksuz dağılımı, son sync zamanı vb.
        /// </summary>
        [HttpGet("cache/stats")]
        public async Task<IActionResult> GetCacheStats()
        {
            try
            {
                var stats = await _cacheService.GetCacheStatsAsync();

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        totalProducts = stats.TotalProducts,
                        activeProducts = stats.ActiveProducts,
                        inactiveProducts = stats.InactiveProducts,
                        productsWithStock = stats.ProductsWithStock,
                        productsWithoutStock = stats.ProductsWithoutStock,
                        syncedProducts = stats.SyncedProducts,
                        notSyncedProducts = stats.NotSyncedProducts,
                        lastSyncTime = stats.LastSyncTime,
                        oldestRecord = stats.OldestRecord,
                        newestRecord = stats.NewestRecord,
                        productsByGrupKod = stats.ProductsByGrupKod
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AdminMicroController] GetCacheStats hatası");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Mikro sync tanılama bilgilerini getirir.
        /// Çakışma adedi, başarı oranı, son hatalar ve pending retry kayıtlarını içerir.
        /// </summary>
        [HttpGet("sync/diagnostics")]
        public async Task<IActionResult> GetSyncDiagnostics([FromQuery] int hours = 24)
        {
            try
            {
                var safeHours = Math.Clamp(hours, 1, 24 * 30); // max 30 gün
                var since = DateTime.UtcNow.AddHours(-safeHours);

                var stats = await _syncLogger.GetStatisticsAsync(since);
                var recentFailures = (await _syncLogger.GetRecentFailuresAsync(safeHours))
                    .OrderByDescending(x => x.CreatedAt)
                    .Take(50)
                    .Select(x => new
                    {
                        x.Id,
                        x.EntityType,
                        x.Direction,
                        x.Status,
                        x.ExternalId,
                        x.InternalId,
                        x.Attempts,
                        x.LastError,
                        x.Message,
                        x.LastAttemptAt,
                        x.CreatedAt
                    })
                    .ToList();

                var pendingRetries = (await _syncLogger.GetPendingRetryLogsAsync())
                    .OrderByDescending(x => x.CreatedAt)
                    .Take(50)
                    .Select(x => new
                    {
                        x.Id,
                        x.EntityType,
                        x.Direction,
                        x.Status,
                        x.ExternalId,
                        x.InternalId,
                        x.Attempts,
                        x.LastError,
                        x.Message,
                        x.LastAttemptAt,
                        x.CreatedAt
                    })
                    .ToList();

                var recentConflicts = (await _syncRepository.GetLogsByDateRangeAsync(
                        since,
                        DateTime.UtcNow,
                        status: "Conflict"))
                    .OrderByDescending(x => x.CreatedAt)
                    .Take(50)
                    .Select(x => new
                    {
                        x.Id,
                        x.EntityType,
                        x.Direction,
                        x.Status,
                        x.ExternalId,
                        x.InternalId,
                        x.Attempts,
                        x.LastError,
                        x.Message,
                        x.LastAttemptAt,
                        x.CreatedAt
                    })
                    .ToList();

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        since,
                        hours = safeHours,
                        summary = new
                        {
                            stats.TotalOperations,
                            stats.SuccessfulOperations,
                            stats.FailedOperations,
                            stats.PendingRetries,
                            stats.ConflictCount,
                            stats.SuccessRate,
                            byEntityType = stats.ByEntityType,
                            byDirection = stats.ByDirection
                        },
                        recentFailures,
                        pendingRetries,
                        recentConflicts
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AdminMicroController] GetSyncDiagnostics hatası");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Belirli bir sync log kaydını manuel retry kuyruğuna alır.
        /// </summary>
        [HttpPost("sync/logs/{logId:int}/retry")]
        public async Task<IActionResult> RetrySyncLog([FromRoute] int logId)
        {
            try
            {
                var log = await _syncRepository.GetLogByIdAsync(logId);
                if (log == null)
                {
                    return NotFound(new { success = false, message = "Log kaydı bulunamadı" });
                }

                await _syncLogger.RetryOperationAsync(logId, "Manuel retry (admin)");

                return Ok(new
                {
                    success = true,
                    message = $"Log #{logId} retry kuyruğuna alındı"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AdminMicroController] RetrySyncLog hatası. LogId: {LogId}", logId);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Conflict logunu manuel çözer.
        /// strategy: erpWins | localWins
        /// </summary>
        [HttpPost("sync/conflicts/{logId:int}/resolve")]
        public async Task<IActionResult> ResolveSyncConflict(
            [FromRoute] int logId,
            [FromQuery] string strategy = "erpWins")
        {
            try
            {
                var normalized = (strategy ?? string.Empty).Trim().ToLowerInvariant();
                if (normalized != "erpwins" && normalized != "localwins")
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Geçersiz strategy. erpWins veya localWins kullanılmalı"
                    });
                }

                var log = await _syncRepository.GetLogByIdAsync(logId);
                if (log == null)
                {
                    return NotFound(new { success = false, message = "Conflict log kaydı bulunamadı" });
                }

                if (!string.Equals(log.Status, "Conflict", StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest(new { success = false, message = "Bu kayıt conflict durumunda değil" });
                }

                if (normalized == "erpwins")
                {
                    await _syncLogger.RetryOperationAsync(logId, "Manual conflict resolve: ERP wins");
                    return Ok(new
                    {
                        success = true,
                        message = $"Conflict #{logId} ERP-Wins ile çözüldü ve retry kuyruğuna alındı"
                    });
                }

                await _syncLogger.CompleteOperationAsync(logId, "Manual conflict resolve: Local wins (ERP update skipped)");
                return Ok(new
                {
                    success = true,
                    message = $"Conflict #{logId} Local-Wins ile kapatıldı"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AdminMicroController] ResolveSyncConflict hatası. LogId: {LogId}", logId);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Cache'i tamamen temizler.
        /// DİKKAT: Tüm cache'lenmiş ürün verileri silinir!
        /// </summary>
        [HttpDelete("cache/clear")]
        public async Task<IActionResult> ClearCache()
        {
            try
            {
                await _cacheService.ClearCacheAsync();

                return Ok(new
                {
                    success = true,
                    message = "Cache temizlendi. Yeniden sync yapmanız gerekiyor."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AdminMicroController] ClearCache hatası");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Server-Sent Events (SSE) ile cache sync işlemini gerçek zamanlı izler.
        /// 
        /// KULLANIM:
        /// Frontend EventSource ile bağlanır ve her sayfa çekildiğinde
        /// progress event'i alır. İşlem bitince "complete" event'i gelir.
        /// 
        /// ÖRNEK:
        /// const evtSource = new EventSource('/api/admin/micro/cache/sync-stream?syncMode=full');
        /// evtSource.onmessage = (e) => console.log(JSON.parse(e.data));
        /// </summary>
        [HttpGet("cache/sync-stream")]
        public async Task SyncProductCacheStream(
            [FromQuery] int fiyatListesiNo = 1,
            [FromQuery] int depoNo = 0,
            [FromQuery] string syncMode = "full",
            CancellationToken cancellationToken = default)
        {
            // SSE için response header'larını ayarla
            Response.Headers.Append("Content-Type", "text/event-stream");
            Response.Headers.Append("Cache-Control", "no-cache");
            Response.Headers.Append("Connection", "keep-alive");
            Response.Headers.Append("X-Accel-Buffering", "no"); // Nginx buffering'i kapat

            try
            {
                _logger.LogInformation(
                    "[AdminMicroController] SSE Cache sync stream başlatılıyor. FiyatListesi: {FiyatListesi}, Depo: {Depo}, Mod: {Mode}",
                    fiyatListesiNo, depoNo, syncMode);

                // Progress callback - her sayfa çekildiğinde tetiklenir
                var progress = new Progress<MikroFetchProgress>(async p =>
                {
                    try
                    {
                        var eventData = System.Text.Json.JsonSerializer.Serialize(new
                        {
                            type = "progress",
                            currentPage = p.CurrentPage,
                            totalPages = p.TotalPages,
                            fetchedCount = p.FetchedCount,
                            totalCount = p.TotalCount,
                            progressPercent = Math.Round(p.ProgressPercentage, 1),
                            elapsedSeconds = Math.Round(p.ElapsedTime.TotalSeconds, 1),
                            estimatedRemainingSeconds = p.EstimatedRemainingTime?.TotalSeconds,
                            status = p.Status
                        });

                        await Response.WriteAsync($"data: {eventData}\n\n", cancellationToken);
                        await Response.Body.FlushAsync(cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "[AdminMicroController] SSE write hatası (client disconnected olabilir)");
                    }
                });

                // Sync işlemini başlat
                var result = await _cacheService.FetchAllAndCacheAsync(fiyatListesiNo, depoNo, progress);

                // İşlem tamamlandı event'i gönder
                var completeData = System.Text.Json.JsonSerializer.Serialize(new
                {
                    type = "complete",
                    success = result.Success,
                    message = result.Message,
                    totalFetched = result.TotalFetched,
                    newProducts = result.NewProducts,
                    updatedProducts = result.UpdatedProducts,
                    unchangedProducts = result.UnchangedProducts,
                    durationSeconds = Math.Round(result.Duration.TotalSeconds, 1),
                    errors = result.Errors
                });

                await Response.WriteAsync($"data: {completeData}\n\n", cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);

                _logger.LogInformation(
                    "[AdminMicroController] SSE Cache sync stream tamamlandı. Toplam: {Total}, Süre: {Duration}s",
                    result.TotalFetched, result.Duration.TotalSeconds);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("[AdminMicroController] SSE stream client tarafından iptal edildi");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AdminMicroController] SSE Cache sync stream hatası");

                try
                {
                    var errorData = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        type = "error",
                        message = ex.Message
                    });
                    await Response.WriteAsync($"data: {errorData}\n\n", cancellationToken);
                    await Response.Body.FlushAsync(cancellationToken);
                }
                catch
                {
                    // Client zaten disconnected olmuş olabilir
                }
            }
        }

        // =====================================================================
        // ÜRÜN BİLGİ SENKRONİZASYONU — Phase 4: Cache → Product info sync
        // NEDEN: HotPoll stok/fiyat dışındaki bilgi değişikliklerini (kategori, birim,
        // barkod, ağırlık, aktif/pasif) Product tablosuna yansıtmaz. Bu endpoint'ler
        // admin panelden tam bilgi senkronizasyonunu ve izlemeyi sağlar.
        // =====================================================================

        /// <summary>
        /// Tüm aktif cache kayıtlarının bilgi alanlarını Product tablosuna senkronize eder.
        /// İsim, slug, birim, ağırlık, kategori ve aktif/pasif durumunu günceller.
        /// </summary>
        [HttpPost("sync/product-info")]
        public async Task<IActionResult> SyncProductInfo(
            [FromServices] IProductInfoSyncService infoSyncService)
        {
            _logger.LogInformation("[AdminSync] Tam ürün bilgi senkronizasyonu tetiklendi — {User}",
                User?.Identity?.Name ?? "unknown");

            var result = await infoSyncService.SyncAllProductInfoAsync(HttpContext.RequestAborted);

            return Ok(new
            {
                success = result.Success,
                totalProcessed = result.TotalProcessed,
                namesUpdated = result.NamesUpdated,
                categoriesUpdated = result.CategoriesUpdated,
                weightInfoUpdated = result.WeightInfoUpdated,
                statusUpdated = result.StatusUpdated,
                skipped = result.Skipped,
                errors = result.Errors,
                durationMs = result.DurationMs,
                errorDetails = result.ErrorDetails.Take(20),
                message = result.Success
                    ? $"Bilgi sync tamamlandı: {result.TotalProcessed} ürün işlendi"
                    : $"Bilgi sync hatası: {result.ErrorMessage}"
            });
        }

        /// <summary>
        /// Kategori eşleme tablosuna göre tüm ürünlerin CategoryId'sini günceller.
        /// MikroCategoryMapping tablosu değiştiğinde çağrılır.
        /// </summary>
        [HttpPost("sync/categories")]
        public async Task<IActionResult> SyncCategories(
            [FromServices] IProductInfoSyncService infoSyncService)
        {
            _logger.LogInformation("[AdminSync] Kategori senkronizasyonu tetiklendi — {User}",
                User?.Identity?.Name ?? "unknown");

            var updated = await infoSyncService.SyncProductCategoriesAsync(HttpContext.RequestAborted);

            return Ok(new
            {
                success = true,
                categoriesUpdated = updated,
                message = $"{updated} ürünün kategorisi güncellendi"
            });
        }

        /// <summary>
        /// Cache'de eşlenmemiş GrupKod değerlerini döner.
        /// Admin panelinde eksik kategori eşleme uyarısı için.
        /// </summary>
        [HttpGet("sync/unmapped-groups")]
        public async Task<IActionResult> GetUnmappedGroups(
            [FromServices] IProductInfoSyncService infoSyncService)
        {
            var unmapped = await infoSyncService.GetUnmappedGroupCodesAsync(HttpContext.RequestAborted);

            return Ok(new
            {
                count = unmapped.Count,
                unmappedGroups = unmapped,
                message = unmapped.Count > 0
                    ? $"{unmapped.Count} grup kodu henüz kategorilere eşlenmemiş"
                    : "Tüm grup kodları eşlenmiş"
            });
        }

        /// <summary>
        /// Resmi eksik olan ürünlerin raporunu döner (image kapsam analizi).
        /// </summary>
        [HttpGet("sync/image-status")]
        public async Task<IActionResult> GetImageSyncStatus(
            [FromServices] IProductInfoSyncService infoSyncService)
        {
            var report = await infoSyncService.GetImageSyncStatusAsync(HttpContext.RequestAborted);

            return Ok(new
            {
                totalProducts = report.TotalProducts,
                withImages = report.ProductsWithImages,
                withoutImages = report.ProductsWithoutImages,
                coveragePercent = report.CoveragePercent,
                missingImageProducts = report.MissingImageProducts
            });
        }

        // =====================================================================
        // MONİTORİNG & METRİKLER — Phase 5: Sync sağlık izleme
        // NEDEN: Sync hataları sessizce birikebilir. Bu endpoint'ler admin
        // dashboard'a sağlık bilgisi, trend verisi ve aktif alert'leri sunar.
        // =====================================================================

        /// <summary>
        /// Tüm sync kanallarının anlık sağlık özetini döner.
        /// Dashboard widget'ı için optimize edilmiştir.
        /// </summary>
        [HttpGet("sync/health")]
        public async Task<IActionResult> GetSyncHealth(
            [FromServices] ISyncMetricsService metricsService)
        {
            var summary = await metricsService.GetHealthSummaryAsync(HttpContext.RequestAborted);

            return Ok(summary);
        }

        /// <summary>
        /// Belirli bir zaman aralığındaki sync metriklerini döner.
        /// Trend grafiği ve performans takibi için.
        /// </summary>
        [HttpGet("sync/metrics")]
        public async Task<IActionResult> GetSyncMetrics(
            [FromQuery] int hours = 24,
            [FromServices] ISyncMetricsService metricsService = null!)
        {
            var report = await metricsService.GetMetricsAsync(hours, HttpContext.RequestAborted);

            return Ok(report);
        }

        /// <summary>
        /// Aktif sync alert'lerini döner (ardışık hata, yüksek hata oranı, sync gecikmesi).
        /// </summary>
        [HttpGet("sync/alerts")]
        public async Task<IActionResult> GetSyncAlerts(
            [FromServices] ISyncMetricsService metricsService)
        {
            var alerts = await metricsService.GetActiveAlertsAsync(HttpContext.RequestAborted);

            return Ok(new
            {
                count = alerts.Count,
                alerts,
                hasCritical = alerts.Any(a => a.Severity == "Critical")
            });
        }

        // =====================================================================
        // SYNC YÖNETİM API'leri — HotPoll durumu, tetikleme, retry
        // NEDEN: Admin panelinden anlık sync durumunu izlemek ve müdahale etmek
        // için. Mikro ERP bağlantı sorunlarında manuel tetikleme gerekebilir.
        // =====================================================================

        /// <summary>
        /// HotPoll servisinin anlık durumunu döner (son başarılı poll zamanı,
        /// hata sayısı, aktif delta penceresi vb.)
        /// </summary>
        [HttpGet("sync/status")]
        public IActionResult GetSyncStatus(
            [FromServices] IMikroHotPollService hotPollService,
            [FromServices] IMikroOutboundSyncService outboundService)
        {
            var status = hotPollService.GetStatus();
            return Ok(new
            {
                hotPoll = new
                {
                    status.IsRunning,
                    status.LastPollTime,
                    status.LastSuccessTime,
                    status.LastPollChangedCount,
                    status.LastPollDurationMs,
                    status.LastError,
                    lastSuccessfulPoll = hotPollService.LastSuccessfulPollTime,
                    consecutiveFailures = hotPollService.ConsecutiveFailureCount,
                },
                timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// HotPoll'u anında çalıştırır — normal 10sn döngüsünü beklemeden delta tarama yapar
        /// </summary>
        [HttpPost("sync/trigger")]
        public async Task<IActionResult> TriggerSync(
            [FromServices] IMikroHotPollService hotPollService)
        {
            _logger.LogInformation("[AdminSync] Manuel HotPoll tetiklendi — {User}",
                User?.Identity?.Name ?? "unknown");

            var result = await hotPollService.PollDeltaChangesAsync(HttpContext.RequestAborted);

            return Ok(new
            {
                success = result.Success,
                totalChanged = result.TotalChanged,
                stockUpdated = result.StockUpdated,
                priceUpdated = result.PriceUpdated,
                infoUpdated = result.InfoUpdated,
                durationMs = result.DurationMs,
                errorMessage = result.ErrorMessage,
                message = result.Success
                    ? $"{result.TotalChanged} ürün güncellendi (Stok: {result.StockUpdated}, Fiyat: {result.PriceUpdated}, Bilgi: {result.InfoUpdated})"
                    : $"Poll başarısız: {result.ErrorMessage}"
            });
        }

        /// <summary>
        /// Başarısız outbound push'ları yeniden dener (EC→Mikro yönü)
        /// Son 24 saatte 3'ten az deneme yapılmış, başarısız olmuş kayıtları bulup retry eder
        /// </summary>
        [HttpPost("sync/retry")]
        public async Task<IActionResult> RetryFailedPushes(
            [FromServices] IMikroOutboundSyncService outboundService)
        {
            _logger.LogInformation("[AdminSync] Manuel retry tetiklendi — {User}",
                User?.Identity?.Name ?? "unknown");

            var result = await outboundService.RetryFailedPushesAsync(HttpContext.RequestAborted);

            return Ok(new
            {
                success = result.Success,
                message = result.Success
                    ? $"{result.PushedCount} push başarıyla retry edildi"
                    : $"Retry kısmen başarısız: {result.Errors?.Count ?? 0} hata",
                successCount = result.PushedCount,
                failureCount = result.FailedCount,
                errors = result.Errors?.Select(e => new { e.StokKod, e.ErrorMessage })
            });
        }

        /// <summary>
        /// Son sync loglarını döner (son N saat, yön filtresiyle)
        /// </summary>
        [HttpGet("sync/logs")]
        public async Task<IActionResult> GetSyncLogs(
            [FromQuery] int hours = 24,
            [FromQuery] string? direction = null,
            [FromQuery] string? status = null)
        {
            hours = Math.Clamp(hours, 1, 168); // max 7 gün

            var startDate = DateTime.UtcNow.AddHours(-hours);
            var logs = await _syncRepository.GetLogsByDateRangeAsync(
                startDate, DateTime.UtcNow, entityType: null, status: status);

            // Yön filtresi (interface parametrede yok, memory'de filtrele)
            var filteredLogs = logs.AsEnumerable();
            if (!string.IsNullOrEmpty(direction))
            {
                filteredLogs = filteredLogs.Where(l =>
                    string.Equals(l.Direction, direction, StringComparison.OrdinalIgnoreCase));
            }

            var result = filteredLogs.Take(500).Select(l => new
            {
                l.Id,
                stokKod = l.ExternalId,
                entityType = l.EntityType,
                l.Direction,
                l.Status,
                error = l.LastError,
                l.Attempts,
                l.CreatedAt,
                l.LastAttemptAt
            }).ToList();

            return Ok(new
            {
                count = result.Count,
                logs = result
            });
        }

        // =====================================================================
        // KATEGORİ EŞLEMESİ CRUD — ADIM 6: Admin panelden mapping yönetimi
        // =====================================================================

        /// <summary>
        /// Tüm kategori eşlemelerini listeler.
        /// </summary>
        [HttpGet("category-mappings")]
        public async Task<IActionResult> GetCategoryMappings(
            [FromServices] IMikroCategoryMappingService mappingService)
        {
            var mappings = await mappingService.GetAllMappingsAsync(HttpContext.RequestAborted);

            var mappingsList = mappings.ToList();

            var categoryIds = mappingsList.Select(m => m.CategoryId).Distinct().ToList();
            var categories = await _context.Categories
                .AsNoTracking()
                .Where(c => categoryIds.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id, c => c.Name, HttpContext.RequestAborted);

            var result = mappingsList.Select(m => new
            {
                m.Id, m.MikroAnagrupKod, m.MikroAltgrupKod, m.MikroMarkaKod,
                m.CategoryId,
                categoryName = categories.ContainsKey(m.CategoryId) ? categories[m.CategoryId] : "?",
                m.BrandId, m.Priority, m.Notes
            });

            return Ok(new { count = mappingsList.Count, mappings = result });
        }

        /// <summary>
        /// Yeni kategori eşlemesi oluşturur veya günceller (upsert).
        /// </summary>
        [HttpPost("category-mappings")]
        public async Task<IActionResult> CreateCategoryMapping(
            [FromBody] CategoryMappingCreateRequest request,
            [FromServices] IMikroCategoryMappingService mappingService)
        {
            if (string.IsNullOrWhiteSpace(request.MikroAnagrupKod))
                return BadRequest(new { error = "MikroAnagrupKod zorunludur" });

            var categoryExists = await _context.Categories
                .AnyAsync(c => c.Id == request.CategoryId, HttpContext.RequestAborted);
            if (!categoryExists)
                return BadRequest(new { error = $"CategoryId={request.CategoryId} bulunamadı" });

            var mapping = new MikroCategoryMapping
            {
                MikroAnagrupKod = request.MikroAnagrupKod.Trim(),
                MikroAltgrupKod = request.MikroAltgrupKod?.Trim(),
                MikroMarkaKod = request.MikroMarkaKod?.Trim(),
                CategoryId = request.CategoryId,
                BrandId = request.BrandId,
                Priority = request.Priority ?? 10,
                Notes = request.Notes
            };

            await mappingService.AddMappingAsync(mapping, HttpContext.RequestAborted);

            _logger.LogInformation(
                "[AdminMapping] Eşleme oluşturuldu: {Anagrup} → CategoryId={CatId} — {User}",
                mapping.MikroAnagrupKod, mapping.CategoryId, User?.Identity?.Name ?? "unknown");

            return Ok(new { success = true, mapping });
        }

        /// <summary>
        /// Mevcut kategori eşlemesini günceller.
        /// </summary>
        [HttpPut("category-mappings/{id}")]
        public async Task<IActionResult> UpdateCategoryMapping(
            int id,
            [FromBody] CategoryMappingCreateRequest request,
            [FromServices] IMikroCategoryMappingService mappingService)
        {
            var existing = await _context.Set<MikroCategoryMapping>()
                .FindAsync(new object[] { id }, HttpContext.RequestAborted);

            if (existing == null)
                return NotFound(new { error = $"Mapping Id={id} bulunamadı" });

            var categoryExists = await _context.Categories
                .AnyAsync(c => c.Id == request.CategoryId, HttpContext.RequestAborted);
            if (!categoryExists)
                return BadRequest(new { error = $"CategoryId={request.CategoryId} bulunamadı" });

            existing.MikroAnagrupKod = request.MikroAnagrupKod?.Trim() ?? existing.MikroAnagrupKod;
            existing.MikroAltgrupKod = request.MikroAltgrupKod?.Trim();
            existing.MikroMarkaKod = request.MikroMarkaKod?.Trim();
            existing.CategoryId = request.CategoryId;
            existing.BrandId = request.BrandId;
            existing.Priority = request.Priority ?? existing.Priority;
            existing.Notes = request.Notes;

            await _context.SaveChangesAsync(HttpContext.RequestAborted);
            await mappingService.InvalidateCacheAsync(HttpContext.RequestAborted);

            // ADIM 11: Mapping değişikliğinden etkilenen ürünleri yeniden kategorile
            int resyncCount = 0;
            try
            {
                var infoSyncService = HttpContext.RequestServices.GetService<IProductInfoSyncService>();
                if (infoSyncService != null && !string.IsNullOrEmpty(existing.MikroAnagrupKod))
                {
                    resyncCount = await infoSyncService.ResyncProductsByAnagrupKodAsync(
                        existing.MikroAnagrupKod, HttpContext.RequestAborted);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[AdminMapping] Mapping güncelleme sonrası resync hatası");
            }

            _logger.LogInformation(
                "[AdminMapping] Mapping güncellendi: Id={Id}, {Anagrup} → CategoryId={CatId}, Resync={Resync}",
                id, existing.MikroAnagrupKod, existing.CategoryId, resyncCount);

            return Ok(new { success = true, mapping = existing, resyncedProducts = resyncCount });
        }

        /// <summary>
        /// Kategori eşlemesini siler.
        /// </summary>
        [HttpDelete("category-mappings/{id}")]
        public async Task<IActionResult> DeleteCategoryMapping(
            int id,
            [FromServices] IMikroCategoryMappingService mappingService)
        {
            var existing = await _context.Set<MikroCategoryMapping>()
                .FindAsync(new object[] { id }, HttpContext.RequestAborted);

            if (existing == null)
                return NotFound(new { error = $"Mapping Id={id} bulunamadı" });

            _context.Set<MikroCategoryMapping>().Remove(existing);
            await _context.SaveChangesAsync(HttpContext.RequestAborted);
            await mappingService.InvalidateCacheAsync(HttpContext.RequestAborted);

            // ADIM 11: Silinen mapping'e bağlı ürünleri yeniden kategorile (wildcard'a düşürür)
            int resyncCount = 0;
            try
            {
                var infoSyncService = HttpContext.RequestServices.GetService<IProductInfoSyncService>();
                if (infoSyncService != null && !string.IsNullOrEmpty(existing.MikroAnagrupKod)
                    && existing.MikroAnagrupKod != "*")
                {
                    resyncCount = await infoSyncService.ResyncProductsByAnagrupKodAsync(
                        existing.MikroAnagrupKod, HttpContext.RequestAborted);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[AdminMapping] Mapping silme sonrası resync hatası");
            }

            _logger.LogInformation("[AdminMapping] Mapping silindi: Id={Id}, {Anagrup}, Resync={Resync}",
                id, existing.MikroAnagrupKod, resyncCount);

            return Ok(new { success = true, deleted = id, resyncedProducts = resyncCount });
        }

        /// <summary>
        /// Tüm eşlenmemiş grup kodlarını otomatik keşfeder ve eşler.
        /// </summary>
        [HttpPost("category-mappings/auto-discover")]
        public async Task<IActionResult> AutoDiscoverMappings(
            [FromServices] IAutoCategoryMappingEngine autoEngine)
        {
            _logger.LogInformation("[AdminMapping] Otomatik keşif tetiklendi — {User}",
                User?.Identity?.Name ?? "unknown");

            var result = await autoEngine.DiscoverAndMapAllAsync(HttpContext.RequestAborted);

            return Ok(new
            {
                success = result.Errors == 0,
                totalGroupCodes = result.TotalGroupCodes,
                alreadyMapped = result.AlreadyMapped,
                newMappingsCreated = result.NewMappingsCreated,
                newCategoriesCreated = result.NewCategoriesCreated,
                fallbackToDiger = result.FallbackToDiger,
                errors = result.Errors,
                errorDetails = result.ErrorDetails.Take(20),
                mappings = result.Mappings
            });
        }

        /// <summary>
        /// Belirli bir grup kodu için kategori önerisi döner.
        /// </summary>
        [HttpGet("category-mappings/suggest")]
        public async Task<IActionResult> SuggestMapping(
            [FromQuery] string anagrupKod,
            [FromQuery] string? altgrupKod,
            [FromServices] IAutoCategoryMappingEngine autoEngine)
        {
            if (string.IsNullOrWhiteSpace(anagrupKod))
                return BadRequest(new { error = "anagrupKod parametresi zorunludur" });

            var suggestions = await autoEngine.SuggestMappingAsync(
                anagrupKod, altgrupKod, HttpContext.RequestAborted);

            return Ok(new { anagrupKod, altgrupKod, suggestions });
        }

        // =====================================================================
        // ADIM 12 — OBSERVABILITY: Kategorileme sağlık durumu ve istatistikleri
        // NEDEN: Sessizce biriken kategorisiz ürünler tespit edilemez hale gelir.
        //        Bu endpoint admin dashboard'da "Kategorisiz Ürün Sayısı" widget'ı besler.
        // =====================================================================

        /// <summary>
        /// Kategorileme istatistikleri — admin dashboard health widget.
        /// Kaç ürün kategorili, kaç ürün "Diğer"de, kaç eşlenmemiş grup var?
        /// </summary>
        [HttpGet("category-stats")]
        public async Task<IActionResult> GetCategoryStats(
            [FromServices] IProductInfoSyncService infoSyncService)
        {
            var ct = HttpContext.RequestAborted;

            // Toplam ürün sayısı
            var totalProducts = await _context.Products.CountAsync(ct);

            // "Diğer" kategorisindeki ürün sayısı
            var digerCategory = await _context.Categories
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Slug == "diger", ct);
            var uncategorizedCount = digerCategory != null
                ? await _context.Products.CountAsync(p => p.CategoryId == digerCategory.Id, ct)
                : 0;

            // Geçerli kategorisi olmayan (orphan) ürünler
            var validCategoryIds = await _context.Categories
                .AsNoTracking()
                .Select(c => c.Id)
                .ToListAsync(ct);
            var validSet = new HashSet<int>(validCategoryIds);
            var orphanCount = await _context.Products
                .CountAsync(p => !validSet.Contains(p.CategoryId), ct);

            // Eşlenmemiş grup kodları
            var unmappedGroups = await infoSyncService.GetUnmappedGroupCodesAsync(ct);
            var unmappedProductCount = unmappedGroups.Sum(g => g.ProductCount);

            // Toplam mapping ve kategori sayısı
            var totalMappings = await _context.Set<MikroCategoryMapping>()
                .CountAsync(m => m.IsActive && m.MikroAnagrupKod != "*", ct);
            var totalCategories = await _context.Categories.CountAsync(c => c.IsActive, ct);

            // Hiyerarşi istatistikleri (ADIM 10)
            var parentCategories = await _context.Categories
                .CountAsync(c => c.IsActive && c.ParentId == null, ct);
            var childCategories = await _context.Categories
                .CountAsync(c => c.IsActive && c.ParentId != null, ct);

            // Sağlık skoru — düşükse uyarı
            var healthPercent = totalProducts > 0
                ? Math.Round((double)(totalProducts - uncategorizedCount - orphanCount) / totalProducts * 100, 1)
                : 100.0;

            // ADIM 12: Observability log — periyodik çağrılarda metrik oluşturur
            if (uncategorizedCount > 50 || orphanCount > 0)
            {
                _logger.LogWarning(
                    "[CategoryStats] ⚠ Kategorisiz ürün sayısı yüksek! " +
                    "Diğer={Uncategorized}, Orphan={Orphan}, UnmappedGroups={Unmapped}",
                    uncategorizedCount, orphanCount, unmappedGroups.Count);
            }

            return Ok(new
            {
                totalProducts,
                categorizedProducts = totalProducts - uncategorizedCount - orphanCount,
                uncategorizedProducts = uncategorizedCount,
                orphanProducts = orphanCount,
                healthPercent,
                totalMappings,
                totalCategories,
                parentCategories,
                childCategories,
                unmappedGroupCount = unmappedGroups.Count,
                unmappedProductCount,
                unmappedGroups = unmappedGroups.Take(20).Select(g => new
                {
                    g.GrupKod,
                    g.AltgrupKod,
                    g.ProductCount,
                    g.SampleStokAd
                })
            });
        }

        /// <summary>
        /// ADIM 7: Tüm mevcut ürünleri yeniden kategoriler (one-time migration).
        /// Auto-Mapping Engine ile keşfet → eşle → Product.CategoryId güncelle.
        /// </summary>
        [HttpPost("category-mappings/recategorize-all")]
        public async Task<IActionResult> RecategorizeAllProducts(
            [FromServices] IProductInfoSyncService infoSyncService)
        {
            _logger.LogInformation(
                "[AdminMapping] Toplu yeniden kategorileme tetiklendi — {User}",
                User?.Identity?.Name ?? "unknown");

            var result = await infoSyncService.RecategorizeAllProductsAsync(HttpContext.RequestAborted);

            return Ok(new
            {
                success = result.Success,
                totalProducts = result.TotalProducts,
                categoriesUpdated = result.CategoriesUpdated,
                newMappingsCreated = result.NewMappingsCreated,
                newCategoriesCreated = result.NewCategoriesCreated,
                fallbackToDiger = result.FallbackToDiger,
                errors = result.Errors,
                durationMs = result.DurationMs,
                errorDetails = result.ErrorDetails.Take(20),
                message = result.Message
            });
        }

        /// <summary>
        /// ADIM 11: Kategori deaktif edildiğinde, ilişkili ürünleri "Diğer"e taşır
        /// ve ilişkili mapping'leri deaktif eder.
        /// NEDEN: FK bütünlüğü — pasif kategorideki ürünler sitede görünmemeye başlar,
        /// ama siparişte sorun yaratır. Güvenli taşıma yapılmalı.
        /// </summary>
        [HttpPost("category-mappings/deactivate-category/{categoryId}")]
        public async Task<IActionResult> DeactivateCategory(
            int categoryId,
            [FromServices] IProductInfoSyncService infoSyncService,
            [FromServices] IMikroCategoryMappingService mappingService)
        {
            var category = await _context.Categories.FindAsync(
                new object[] { categoryId }, HttpContext.RequestAborted);

            if (category == null)
                return NotFound(new { error = $"CategoryId={categoryId} bulunamadı" });

            if (category.Slug == "diger")
                return BadRequest(new { error = "'Diğer' kategorisi deaktif edilemez — sistem güvenliği için gerekli" });

            // 1. Ürünleri "Diğer"e taşı
            var movedCount = await infoSyncService.MoveProductsToDigerAsync(
                categoryId, HttpContext.RequestAborted);

            // 2. İlişkili mapping'leri deaktif et
            var relatedMappings = await _context.Set<MikroCategoryMapping>()
                .Where(m => m.CategoryId == categoryId)
                .ToListAsync(HttpContext.RequestAborted);

            foreach (var mapping in relatedMappings)
            {
                mapping.IsActive = false;
                mapping.UpdatedAt = DateTime.UtcNow;
            }

            // 3. Kategoriyi deaktif et
            category.IsActive = false;
            category.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(HttpContext.RequestAborted);
            await mappingService.InvalidateCacheAsync(HttpContext.RequestAborted);

            _logger.LogInformation(
                "[AdminMapping] Kategori deaktif edildi: Id={Id}, {Name}. " +
                "Taşınan ürün: {Moved}, Deaktif mapping: {Mappings}",
                categoryId, category.Name, movedCount, relatedMappings.Count);

            return Ok(new
            {
                success = true,
                categoryId,
                categoryName = category.Name,
                movedProducts = movedCount,
                deactivatedMappings = relatedMappings.Count
            });
        }
    }

    /// <summary>
    /// Kategori eşleme oluşturma/güncelleme request DTO'su
    /// </summary>
    public class CategoryMappingCreateRequest
    {
        public string? MikroAnagrupKod { get; set; }
        public string? MikroAltgrupKod { get; set; }
        public string? MikroMarkaKod { get; set; }
        public int CategoryId { get; set; }
        public int? BrandId { get; set; }
        public int? Priority { get; set; }
        public string? Notes { get; set; }
    }

    /// <summary>
    /// Toplu aktif/pasif değişiklik için request DTO'su
    /// </summary>
    public class BulkToggleActiveRequest
    {
        /// <summary>
        /// İşlem yapılacak stok kodları listesi
        /// </summary>
        public List<string> StokKodlar { get; set; } = new();

        /// <summary>
        /// Yeni aktiflik durumu (true = aktif, false = pasif)
        /// </summary>
        public bool Aktif { get; set; }
    }

    /// <summary>
    /// Excel import satır sonucu DTO'su.
    /// Her satır için işlem durumunu ve hata mesajını içerir.
    /// </summary>
    public class ExcelImportRow
    {
        /// <summary>
        /// Excel'deki satır numarası
        /// </summary>
        public int SatirNo { get; set; }

        /// <summary>
        /// Stok kodu
        /// </summary>
        public string StokKod { get; set; } = "";

        /// <summary>
        /// Aktiflik durumu (Excel'den parse edilmiş)
        /// </summary>
        public bool Aktif { get; set; }

        /// <summary>
        /// İşlem başarılı mı?
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// İşlem mesajı (başarı veya hata açıklaması)
        /// </summary>
        public string Mesaj { get; set; } = "";
    }

    /// <summary>
    /// Ürün bilgilerini Mikro'ya sync etmek için request DTO'su.
    /// İsim, açıklama, birim gibi temel bilgileri içerir.
    /// </summary>
    public class MikroProductUpdateRequest
    {
        /// <summary>
        /// Stok kodu (zorunlu)
        /// </summary>
        public string StokKod { get; set; } = "";

        /// <summary>
        /// Ürün adı
        /// </summary>
        public string? StokAd { get; set; }

        /// <summary>
        /// Barkod numarası
        /// </summary>
        public string? Barkod { get; set; }

        /// <summary>
        /// Birim adı (ADET, KG, LT vb.)
        /// </summary>
        public string? Birim { get; set; }

        /// <summary>
        /// KDV oranı (%)
        /// </summary>
        public decimal? KdvOrani { get; set; }

        /// <summary>
        /// Grup/kategori kodu
        /// </summary>
        public string? GrupKod { get; set; }

        /// <summary>
        /// Ürün açıklaması
        /// </summary>
        public string? Aciklama { get; set; }

        /// <summary>
        /// Ürün resim URL'si
        /// </summary>
        public string? ResimUrl { get; set; }
    }

    /// <summary>
    /// Ürün fiyatını Mikro'ya sync etmek için request DTO'su.
    /// </summary>
    public class MikroPriceUpdateRequest
    {
        /// <summary>
        /// Stok kodu (zorunlu)
        /// </summary>
        public string StokKod { get; set; } = "";

        /// <summary>
        /// Yeni fiyat değeri
        /// </summary>
        public decimal YeniFiyat { get; set; }

        /// <summary>
        /// Fiyat listesi numarası (1-10). Varsayılan: 1
        /// </summary>
        public int? FiyatListesiNo { get; set; }

        /// <summary>
        /// KDV dahil mi? Varsayılan: true
        /// </summary>
        public bool? KdvDahil { get; set; }
    }

    /// <summary>
    /// Tam ürün senkronizasyonu için request DTO'su.
    /// Ürün bilgileri + fiyat birlikte güncellenir.
    /// </summary>
    public class MikroFullUpdateRequest
    {
        /// <summary>
        /// Stok kodu (zorunlu)
        /// </summary>
        public string StokKod { get; set; } = "";

        /// <summary>
        /// Ürün adı
        /// </summary>
        public string? StokAd { get; set; }

        /// <summary>
        /// Barkod numarası
        /// </summary>
        public string? Barkod { get; set; }

        /// <summary>
        /// Birim adı
        /// </summary>
        public string? Birim { get; set; }

        /// <summary>
        /// KDV oranı (%)
        /// </summary>
        public decimal? KdvOrani { get; set; }

        /// <summary>
        /// Grup/kategori kodu
        /// </summary>
        public string? GrupKod { get; set; }

        /// <summary>
        /// Ürün açıklaması
        /// </summary>
        public string? Aciklama { get; set; }

        /// <summary>
        /// Ürün resim URL'si
        /// </summary>
        public string? ResimUrl { get; set; }

        /// <summary>
        /// Satış fiyatı
        /// </summary>
        public decimal? SatisFiyati { get; set; }

        /// <summary>
        /// Fiyat listesi numarası
        /// </summary>
        public int? FiyatListesiNo { get; set; }

        /// <summary>
        /// KDV dahil mi?
        /// </summary>
        public bool? KdvDahil { get; set; }
    }
}
