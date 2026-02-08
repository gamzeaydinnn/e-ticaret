using Microsoft.AspNetCore.Authorization;
using ECommerce.Core.Constants;
using Microsoft.AspNetCore.Mvc;
using ECommerce.Business.Services.Managers;
using ECommerce.Core.Interfaces;
using ECommerce.Core.DTOs.Micro;
using ECommerce.Entities.Concrete;
using ECommerce.Infrastructure.Services.MicroServices;
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
        private readonly ILogger<AdminMicroController> _logger;

        public AdminMicroController(
            MicroSyncManager microSyncManager, 
            IMicroService microService,
            MikroApiService mikroApiService,
            IProductRepository productRepository,
            ILogger<AdminMicroController> logger)
        {
            _microSyncManager = microSyncManager;
            _microService = microService;
            _mikroApiService = mikroApiService;
            _productRepository = productRepository;
            _logger = logger;
        }

        //--- Yönetici Yetkisi Gerektiren İşlemler (Mutating/Triggering) ---
        
        /// <summary>
        /// Mikro ERP’ye tüm ürünleri senkronize et (Yönetici yetkisi gereklidir)
        /// </summary>
        [HttpPost("sync-products")]
        public IActionResult SyncProducts()
        {
            // Bu kritik işlem sadece AdminController altında olmalı
            _microSyncManager.SyncProductsToMikro(); 
            return Ok(new { message = "Ürünler Mikro ERP ile senkronize edildi." });
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
        public async Task<IActionResult> GetProducts()
        {
            var products = await _microService.GetProductsAsync();
            return Ok(products);
        }

        /// <summary>
        /// Mikro ERP’den stokları getir (Admin sayfasında gösterim için)
        /// </summary>
        [HttpGet("stocks")]
        public async Task<IActionResult> GetStocks()
        {
            var stocks = await _microService.GetStocksAsync();
            return Ok(stocks);
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
        /// </summary>
        /// <param name="sayfa">Sayfa numarası (varsayılan: 1)</param>
        /// <param name="sayfaBuyuklugu">Sayfa büyüklüğü (varsayılan: 50)</param>
        /// <param name="depoNo">Depo numarası (varsayılan: 1)</param>
        /// <param name="stokKod">Stok kodu filtresi (opsiyonel)</param>
        /// <param name="grupKod">Grup kodu filtresi (opsiyonel)</param>
        /// <param name="sadeceAktif">Sadece aktif ürünler (varsayılan: true)</param>
        [HttpGet("stok-listesi")]
        public async Task<IActionResult> GetStokListesiV2(
            [FromQuery] int sayfa = 1,
            [FromQuery] int sayfaBuyuklugu = 50,
            [FromQuery] int depoNo = 1,
            [FromQuery] string? stokKod = null,
            [FromQuery] string? grupKod = null,
            [FromQuery] bool sadeceAktif = true)
        {
            try
            {
                // Request DTO'sunu oluştur - MikroAPI V2 formatına uygun
                var request = new MikroStokListesiRequestDto
                {
                    SayfaNo = sayfa,
                    SayfaBuyuklugu = sayfaBuyuklugu,
                    DepoNo = depoNo,
                    StokKod = stokKod ?? string.Empty,
                    GrupKodu = grupKod,
                    PasifDahil = !sadeceAktif // Ters mantık: sadeceAktif=true ise PasifDahil=false
                };

                _logger.LogInformation(
                    "[AdminMicroController] StokListesiV2 çağrılıyor. Sayfa: {Sayfa}, Büyüklük: {Buyukluk}, Depo: {Depo}",
                    sayfa, sayfaBuyuklugu, depoNo);

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
                var toplamKayit = result.TotalCount ?? 0;
                
                return Ok(new
                {
                    success = true,
                    sayfa = sayfa,
                    sayfaBuyuklugu = sayfaBuyuklugu,
                    toplamKayit = toplamKayit,
                    toplamSayfa = sayfaBuyuklugu > 0 ? (int)Math.Ceiling((decimal)toplamKayit / sayfaBuyuklugu) : 0,
                    kayitSayisi = data.Count,
                    depoNo = depoNo,
                    data = data.Select(s => new
                    {
                        stokKod = s.StoKod,
                        stokAd = s.StoIsim,
                        barkod = s.Barkod,
                        grupKod = s.GrupKodu,
                        birim = s.BirimAdi,
                        kdvOrani = s.KdvOrani,
                        satisFiyati = s.SatisFiyat1,
                        depoMiktari = s.DepoMiktar,
                        satilabilirMiktar = s.KullanilabilirMiktar,
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
                    SayfaBuyuklugu = 1
                };

                var result = await _mikroApiService.GetStokListesiV2Async(request);

                if (result.Success)
                {
                    return Ok(new
                    {
                        success = true,
                        message = "Mikro API bağlantısı başarılı!",
                        toplamUrunSayisi = result.TotalCount,
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
    }
}
