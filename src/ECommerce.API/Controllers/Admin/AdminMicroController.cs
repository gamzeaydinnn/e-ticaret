using Microsoft.AspNetCore.Authorization;
using ECommerce.Core.Constants;
using Microsoft.AspNetCore.Mvc;
using ECommerce.Business.Services.Managers;
using ECommerce.Core.Interfaces;
using ECommerce.Core.DTOs.Micro;
using ECommerce.Entities.Concrete;
using ECommerce.Infrastructure.Services.MicroServices;
using ECommerce.API.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

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
        private readonly ILogger<AdminMicroController> _logger;

        public AdminMicroController(
            MicroSyncManager microSyncManager, 
            IMicroService microService,
            MikroApiService mikroApiService,
            IProductRepository productRepository,
            IMikroProductCacheService cacheService,
            ILogger<AdminMicroController> logger)
        {
            _microSyncManager = microSyncManager;
            _microService = microService;
            _mikroApiService = mikroApiService;
            _productRepository = productRepository;
            _cacheService = cacheService;
            _logger = logger;
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

            // Frontend timeout'larını önlemek için varsayılan kaynak DB ve sayfalı yanıttır.
            // source=erp verilirse de yine sayfalı veri çekilir.
            if (string.Equals(source, "erp", StringComparison.OrdinalIgnoreCase))
            {
                var request = new MikroStokListesiRequestDto
                {
                    SayfaNo = page,
                    SayfaBuyuklugu = perPage,
                    FiyatDahil = true,
                    BarkodDahil = true,
                    PasifDahil = false
                };

                var result = await _mikroApiService.GetStokListesiV2Async(request);
                if (!result.Success)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = result.Message ?? "Mikro API hatası"
                    });
                }

                var data = result.Data ?? new List<MikroStokResponseDto>();
                return Ok(new
                {
                    success = true,
                    source = "erp",
                    page,
                    perPage,
                    totalCount = result.TotalCount ?? 0,
                    count = data.Count,
                    data = data.Select(s => new
                    {
                        sku = s.StoKod,
                        name = s.StoIsim,
                        price = ResolveProductPrice(s),
                        stockQuantity = ResolveStockQuantity(s),
                        isActive = s.Aktif ?? true
                    })
                });
            }

            var dbProducts = (await _productRepository.GetAllAsync()).ToList();
            var totalCount = dbProducts.Count;
            var paged = dbProducts
                .Skip((page - 1) * perPage)
                .Take(perPage)
                .ToList();

            var products = paged.Select(p => new MicroProductDto
            {
                Sku = p.SKU ?? $"PRD-{p.Id}",
                Name = p.Name,
                Barcode = string.Empty,
                Price = p.Price,
                VatRate = 20,
                StockQuantity = p.StockQuantity,
                CategoryCode = p.Category?.Name ?? "Genel",
                Unit = "ADET",
                IsActive = p.IsActive,
                LastModified = p.UpdatedAt ?? p.CreatedAt
            }).ToList();

            return Ok(new
            {
                success = true,
                source = "db",
                page,
                perPage,
                totalCount,
                count = products.Count,
                data = products
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

            // Frontend timeout'larını önlemek için varsayılan kaynak DB ve sayfalı yanıttır.
            // source=erp verilirse de yine sayfalı veri çekilir.
            if (string.Equals(source, "erp", StringComparison.OrdinalIgnoreCase))
            {
                var request = new MikroStokListesiRequestDto
                {
                    SayfaNo = page,
                    SayfaBuyuklugu = perPage,
                    FiyatDahil = true,
                    BarkodDahil = true,
                    PasifDahil = false
                };

                var result = await _mikroApiService.GetStokListesiV2Async(request);
                if (!result.Success)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = result.Message ?? "Mikro API hatası"
                    });
                }

                var data = result.Data ?? new List<MikroStokResponseDto>();
                return Ok(new
                {
                    success = true,
                    source = "erp",
                    page,
                    perPage,
                    totalCount = result.TotalCount ?? 0,
                    count = data.Count,
                    data = data.Select(s => new
                    {
                        sku = s.StoKod,
                        quantity = ResolveStockQuantity(s),
                        availableQuantity = ResolveAvailableQuantity(s),
                        reservedQuantity = (int)(s.RezervedMiktar ?? 0)
                    })
                });
            }

            var dbProducts = (await _productRepository.GetAllAsync()).ToList();
            var totalCount = dbProducts.Count;
            var paged = dbProducts
                .Skip((page - 1) * perPage)
                .Take(perPage)
                .ToList();

            var stocks = paged.Select(p => new MicroStockDto
            {
                Sku = p.SKU ?? $"PRD-{p.Id}",
                Barcode = string.Empty,
                Quantity = p.StockQuantity,
                AvailableQuantity = p.StockQuantity,
                ReservedQuantity = 0,
                WarehouseCode = "DEPO0",
                LastUpdated = p.UpdatedAt ?? p.CreatedAt
            }).ToList();

            return Ok(new
            {
                success = true,
                source = "db",
                page,
                perPage,
                totalCount,
                count = stocks.Count,
                data = stocks
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
        /// NEDEN fiyatListesiNo eklendi: Mikro ERP'de 10'a kadar farklı fiyat listesi tanımlanabilir.
        /// Her liste farklı müşteri gruplarına (perakende, toptan, bayi vb.) hitap eder.
        /// Kullanıcı istediği listeyi seçerek doğru fiyatları görebilir.
        /// </summary>
        /// <param name="sayfa">Sayfa numarası (varsayılan: 1)</param>
        /// <param name="sayfaBuyuklugu">Sayfa büyüklüğü (varsayılan: 50)</param>
        /// <param name="depoNo">Depo numarası (varsayılan: 0 = Tüm depolar)</param>
        /// <param name="fiyatListesiNo">Fiyat listesi numarası (1-10 arası, varsayılan: 1 = Perakende)</param>
        /// <param name="stokKod">Stok kodu filtresi (opsiyonel)</param>
        /// <param name="grupKod">Grup kodu filtresi (opsiyonel)</param>
        /// <param name="sadeceAktif">Sadece aktif ürünler (varsayılan: true)</param>
        [HttpGet("stok-listesi")]
        public async Task<IActionResult> GetStokListesiV2(
            [FromQuery] int sayfa = 1,
            [FromQuery] int sayfaBuyuklugu = 50,
            [FromQuery] int depoNo = 0,
            [FromQuery] int fiyatListesiNo = 1,
            [FromQuery] string? stokKod = null,
            [FromQuery] string? grupKod = null,
            [FromQuery] bool sadeceAktif = true)
        {
            try
            {
                // Fiyat listesi numarası validasyonu (1-10 arası olmalı)
                fiyatListesiNo = Math.Clamp(fiyatListesiNo, 1, 10);
                
                // Request DTO'sunu oluştur - MikroAPI V2 formatına uygun
                var request = new MikroStokListesiRequestDto
                {
                    SayfaNo = sayfa,
                    SayfaBuyuklugu = sayfaBuyuklugu,
                    DepoNo = depoNo > 0 ? depoNo : null, // 0 = tüm depolar (null gönder)
                    StokKod = stokKod ?? string.Empty,
                    GrupKodu = grupKod,
                    PasifDahil = !sadeceAktif, // Ters mantık: sadeceAktif=true ise PasifDahil=false
                    FiyatDahil = true,         // Fiyatları mutlaka dahil et
                    BarkodDahil = true
                };

                _logger.LogInformation(
                    "[AdminMicroController] StokListesiV2 çağrılıyor. Sayfa: {Sayfa}, Büyüklük: {Buyukluk}, Depo: {Depo}, FiyatListesi: {FiyatListesi}",
                    sayfa, sayfaBuyuklugu, depoNo, fiyatListesiNo);

                var result = await _mikroApiService.GetStokListesiV2Async(request);

                if (!result.Success)
                {
                    _logger.LogWarning(
                        "[AdminMicroController] StokListesiV2 başarısız. Success: {Success}, Message: {Message}, TotalCount: {Total}",
                        result.Success, result.Message ?? "(boş)", result.TotalCount);

                    return BadRequest(new
                    {
                        success = false,
                        message = result.Message ?? "Mikro API hatası - Success=false"
                    });
                }

                // Veriyi döndür
                var data = result.Data ?? new List<MikroStokResponseDto>();
                var toplamKayit = (result.TotalCount.HasValue && result.TotalCount.Value > 0)
                    ? result.TotalCount.Value
                    : data.Count;
                
                return Ok(new
                {
                    success = true,
                    sayfa = sayfa,
                    sayfaBuyuklugu = sayfaBuyuklugu,
                    toplamKayit = toplamKayit,
                    toplamSayfa = sayfaBuyuklugu > 0 ? (int)Math.Ceiling((decimal)toplamKayit / sayfaBuyuklugu) : 0,
                    kayitSayisi = data.Count,
                    depoNo = depoNo,
                    fiyatListesiNo = fiyatListesiNo,
                    data = data.Select(s => new
                    {
                        stokKod = s.StoKod,
                        stokAd = s.StoIsim,
                        barkod = s.Barkod,
                        grupKod = s.GrupKodu,
                        birim = s.BirimAdi,
                        kdvOrani = s.KdvOrani,
                        // Seçilen fiyat listesinden fiyat al
                        satisFiyati = ResolveProductPrice(s, fiyatListesiNo),
                        // Depo stoğu: önce belirli depo, yoksa toplam stok
                        depoMiktari = ResolveStockQuantity(s, depoNo),
                        satilabilirMiktar = ResolveAvailableQuantity(s, depoNo),
                        // Tüm fiyat listelerini de gönder (debug/karşılaştırma için)
                        tumFiyatlar = s.SatisFiyatlari?.Select(f => new {
                            listeNo = f.SfiyatNo,
                            aciklama = f.SfiyatAciklama,
                            fiyat = f.SfiyatFiyati,
                            kdvDahil = f.SfiyatVergiDahil,
                            dovizCinsi = f.SfiyatDovizCinsi
                        }),
                        aktif = s.Aktif,
                        aciklama = s.StoKisaIsmi
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AdminMicroController] StokListesiV2 hatası - Veritabanından fallback yapılıyor");
                
                // === FALLBACK: Mikro API offline ise veritabanından ürünleri getir ===
                try
                {
                    var dbProducts = await _productRepository.GetAllAsync();
                    var productList = dbProducts.ToList();
                    
                    // Sayfalama uygula
                    var pagedProducts = productList
                        .Skip((sayfa - 1) * sayfaBuyuklugu)
                        .Take(sayfaBuyuklugu)
                        .ToList();

                    return Ok(new
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
                            kdvOrani = 20, // Varsayılan KDV
                            satisFiyati = p.Price,
                            depoMiktari = p.StockQuantity,
                            satilabilirMiktar = p.StockQuantity,
                            aktif = p.IsActive,
                            aciklama = p.Description ?? ""
                        })
                    });
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
        /// Mikro API bağlantı testi.
        /// API ayarlarını ve bağlantıyı doğrular.
        /// </summary>
        [HttpGet("test-connection")]
        public async Task<IActionResult> TestConnection()
        {
            try
            {
                _logger.LogInformation("[AdminMicroController] Mikro API bağlantı testi başlatılıyor");

                // Küçük bir sayfa ile test et
                var request = new MikroStokListesiRequestDto
                {
                    SayfaNo = 1,
                    SayfaBuyuklugu = 20
                };

                var result = await _mikroApiService.GetStokListesiV2Async(request);
                var fetchedCount = result.Data?.Count ?? 0;
                var totalProducts = (result.TotalCount.HasValue && result.TotalCount.Value > 0)
                    ? result.TotalCount.Value
                    : fetchedCount;

                if (result.Success)
                {
                    return Ok(new
                    {
                        success = true,
                        message = "Mikro API bağlantısı başarılı!",
                        toplamUrunSayisi = totalProducts,
                        cekilenKayitSayisi = fetchedCount,
                        apiVersion = "V2",
                        timestamp = DateTime.UtcNow
                    });
                }
                else
                {
                    return Ok(new
                    {
                        success = false,
                        message = result.Message ?? "Bağlantı başarısız",
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
        /// 2. İlk fiyat > 0 olan liste (SfiyatFiyati > 0)
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
                return 0m;
            }

            // 1. Öncelik: Seçilen fiyat listesi numarasından al
            var selectedPrice = stok.SatisFiyatlari
                .FirstOrDefault(f => f.SfiyatNo == fiyatListesiNo);
            
            if (selectedPrice != null && selectedPrice.SfiyatFiyati > 0)
            {
                return selectedPrice.SfiyatFiyati;
            }

            // 2. Fallback: İlk fiyatı > 0 olan listeyi al (sıralı)
            var fallbackPrice = stok.SatisFiyatlari
                .OrderBy(f => f.SfiyatNo)
                .FirstOrDefault(f => f.SfiyatFiyati > 0);

            if (fallbackPrice != null && fallbackPrice.SfiyatFiyati > 0)
            {
                return fallbackPrice.SfiyatFiyati;
            }

            // 3. Son çare: Son alış fiyatı
            if (stok.StoSonAlis.HasValue && stok.StoSonAlis.Value > 0)
            {
                return stok.StoSonAlis.Value;
            }

            return 0m;
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
            // Belirli bir depo seçildiyse, o deponun stoğunu ara
            if (depoNo > 0 && stok.DepoStoklari != null && stok.DepoStoklari.Count > 0)
            {
                var depoStok = stok.DepoStoklari.FirstOrDefault(d => d.DepNo == depoNo);
                if (depoStok != null)
                {
                    return (int)Math.Max(0, Math.Floor(depoStok.StokMiktar));
                }
            }

            // Depo bazlı miktar varsa onu kullan
            if (stok.DepoMiktar.HasValue && stok.DepoMiktar.Value > 0)
            {
                return (int)Math.Max(0, Math.Floor(stok.DepoMiktar.Value));
            }

            // Toplam stok miktarı
            if (stok.StoMiktar > 0)
            {
                return (int)Math.Max(0, Math.Floor(stok.StoMiktar));
            }

            // Tüm depoların toplamını hesapla
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
            [FromQuery] string? sortBy = "stokKod",
            [FromQuery] bool sortDesc = false)
        {
            try
            {
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
    }
}
