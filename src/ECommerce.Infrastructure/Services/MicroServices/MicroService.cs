using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Security.Cryptography;
using ECommerce.Core.DTOs.Micro;
using ECommerce.Core.Interfaces;
using ECommerce.Entities.Concrete;
using System;
using System.Net.Http.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using ECommerce.Infrastructure.Config;

namespace ECommerce.Infrastructure.Services.MicroServices
{
    /// <summary>
    /// MikroAPI V2 ile entegrasyon servisi.
    /// Bu sınıf, Mikro ERP sistemi ile çift yönlü veri alışverişini sağlar.
    /// 
    /// ÖNEMLİ NOTLAR:
    /// - MikroAPI V2 endpoint'leri { "Mikro": { ... } } wrapper formatı bekler
    /// - Şifre her gün "YYYY-MM-DD" + BOŞLUK + "PlainPassword" formatında MD5 hash'lenir
    /// - Her istekte ApiKey, FirmaKodu, KullaniciKodu, CalismaYili ve hash'lenmiş Sifre gönderilir
    /// 
    /// Endpoint'ler:
    /// - /Api/APIMethods/StokListesiV2 → Stok/Ürün çekme
    /// - /Api/apiMethods/SiparisKaydetV2 → Sipariş gönderme
    /// - /Api/APIMethods/CariKaydetV2 → Müşteri kayıt
    /// - /Api/apiMethods/FiyatDegisikligiKaydetV2 → Fiyat güncelleme
    /// </summary>
    public class MicroService : IMicroService
    {
        // ==================== BAĞIMLILIKLAR ====================
        private readonly HttpClient _httpClient;
        private readonly MikroSettings _settings;
        private readonly ILogger<MicroService> _logger;
        
        // JSON serileştirme ayarları - MikroAPI'nin beklediği format için
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = null, // PascalCase koru (Mikro bunu bekliyor)
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false // Performans için
        };

        // ==================== CONSTRUCTOR ====================
        public MicroService(
            HttpClient httpClient, 
            IOptions<MikroSettings> settings,
            ILogger<MicroService> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            // HttpClient timeout ayarı
            _httpClient.Timeout = TimeSpan.FromSeconds(_settings.RequestTimeoutSeconds);
            
            _logger.LogInformation(
                "[MicroService] Başlatıldı. API URL: {ApiUrl}, Firma: {FirmaKodu}, Yıl: {CalismaYili}",
                _settings.ApiUrl, _settings.FirmaKodu, _settings.CalismaYili);
        }

        // ==================== MD5 HASH MEKANİZMASI ====================
        
        /// <summary>
        /// MikroAPI için günlük şifre hash'i oluşturur.
        /// Format: "YYYY-MM-DD" + BOŞLUK + PlainPassword → MD5 (lowercase hex)
        /// 
        /// ÖRNEK:
        /// - Tarih: 2026-02-03
        /// - Şifre: 123asd
        /// - Birleşik: "2026-02-03 123asd" (boşluk ile!)
        /// - MD5 Hash: hesaplanmış hash
        /// </summary>
        /// <param name="plainPassword">Şifre (plain text)</param>
        /// <returns>MD5 hash (32 karakter, lowercase hex)</returns>
        private string GenerateDailyPasswordHash(string plainPassword)
        {
            if (string.IsNullOrEmpty(plainPassword))
            {
                _logger.LogWarning("[MicroService] Şifre boş! MD5 hash üretilemedi.");
                return string.Empty;
            }

            // MikroAPI formatı: YYYY-MM-DD + boşluk + şifre
            var today = DateTime.Now.ToString("yyyy-MM-dd");
            var dataToHash = today + " " + plainPassword;  // ✅ Boşluk karakteri eklendi
            
            // MD5 hash hesapla
            using var md5 = MD5.Create();
            var inputBytes = Encoding.UTF8.GetBytes(dataToHash);
            var hashBytes = md5.ComputeHash(inputBytes);
            
            // Hex string'e çevir (lowercase)
            var hashString = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            
            _logger.LogDebug("[MicroService] MD5 hash oluşturuldu. Tarih: {Date}", today);
            
            return hashString;
        }

        // ==================== MIKRO AUTH WRAPPER ====================
        
        /// <summary>
        /// MikroAPI V2 için authentication wrapper objesi oluşturur.
        /// Tüm V2 endpoint'leri bu formatta request body bekler.
        /// </summary>
        private MikroAuthWrapper CreateAuthWrapper()
        {
            return new MikroAuthWrapper
            {
                ApiKey = _settings.ApiKey,
                CalismaYili = _settings.CalismaYili,
                FirmaKodu = _settings.FirmaKodu,
                KullaniciKodu = _settings.KullaniciKodu,
                Sifre = GenerateDailyPasswordHash(_settings.Sifre)
                // NOT: FirmaNo ve SubeNo Mikro objesinin içinde değil, 
                // request root seviyesinde veya hiç gönderilmez
            };
        }

        /// <summary>
        /// MikroAPI V2 formatında request body oluşturur.
        /// DOĞRU FORMAT: { "Mikro": { auth }, "Index": 0, "Size": "100", ... }
        /// Auth bilgileri Mikro içinde, diğer parametreler ROOT seviyede olmalı!
        /// </summary>
        private object CreateMikroRequest(object? additionalData = null)
        {
            var auth = CreateAuthWrapper();
            
            // Eğer ek veri yoksa, sadece auth wrapper döndür
            if (additionalData == null)
            {
                return new { Mikro = auth };
            }
            
            // MikroAPI V2 formatı: Auth bilgileri Mikro içinde, diğer parametreler ROOT seviyede
            // { "Mikro": { auth }, "Index": 0, "Size": "100", "Sort": "sto_kod", ... }
            var rootLevel = new Dictionary<string, object?>
            {
                ["Mikro"] = auth
            };

            // Ek veriyi dictionary'ye dönüştür ve ROOT seviyeye ekle
            var additionalDict = JsonSerializer.Deserialize<Dictionary<string, object>>(
                JsonSerializer.Serialize(additionalData, _jsonOptions), _jsonOptions);
            
            if (additionalDict != null)
            {
                foreach (var kvp in additionalDict)
                {
                    // Null değerleri atlama
                    if (kvp.Value != null)
                    {
                        rootLevel[kvp.Key] = kvp.Value;
                    }
                }
            }

            return rootLevel;
        }

        // ==================== HTTP İSTEK GÖNDERİM ====================
        
        /// <summary>
        /// MikroAPI'ye POST isteği gönderir ve retry mekanizması uygular.
        /// 
        /// RETRY POLİTİKASI:
        /// - 5XX hataları → Yeniden dene (exponential backoff)
        /// - 4XX hataları → Yeniden deneme (client hatası)
        /// - Network hataları → Yeniden dene
        /// </summary>
        private async Task<HttpResponseMessage> SendMikroRequestAsync(
            string endpoint, 
            object requestBody,
            CancellationToken cancellationToken = default)
        {
            var maxAttempts = _settings.MaxRetryAttempts;
            Exception? lastException = null;
            
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    _logger.LogDebug(
                        "[MicroService] İstek gönderiliyor. Endpoint: {Endpoint}, Deneme: {Attempt}/{Max}",
                        endpoint, attempt, maxAttempts);
                    
                    // Request body'yi JSON'a serialize et
                    var jsonContent = JsonSerializer.Serialize(requestBody, _jsonOptions);
                    var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                    
                    // POST isteği gönder
                    var response = await _httpClient.PostAsync(endpoint, content, cancellationToken);
                    
                    // Başarılı veya client hatası (4XX) ise döndür
                    if (response.IsSuccessStatusCode || (int)response.StatusCode < 500)
                    {
                        _logger.LogInformation(
                            "[MicroService] İstek tamamlandı. Endpoint: {Endpoint}, Status: {Status}",
                            endpoint, response.StatusCode);
                        return response;
                    }
                    
                    // 5XX hatası - retry gerekli
                    _logger.LogWarning(
                        "[MicroService] Sunucu hatası. Endpoint: {Endpoint}, Status: {Status}, Deneme: {Attempt}",
                        endpoint, response.StatusCode, attempt);
                    
                    if (attempt < maxAttempts)
                    {
                        // Exponential backoff: 250ms, 1s, 2.25s
                        var delay = TimeSpan.FromMilliseconds(250 * attempt * attempt);
                        await Task.Delay(delay, cancellationToken);
                    }
                    else
                    {
                        return response; // Son denemede de başarısızsa response'u döndür
                    }
                }
                catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
                {
                    _logger.LogError(ex, "[MicroService] Timeout hatası. Endpoint: {Endpoint}", endpoint);
                    lastException = ex;
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogError(ex, "[MicroService] HTTP hatası. Endpoint: {Endpoint}", endpoint);
                    lastException = ex;
                    
                    if (attempt < maxAttempts)
                    {
                        var delay = TimeSpan.FromMilliseconds(250 * attempt * attempt);
                        await Task.Delay(delay, cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[MicroService] Beklenmeyen hata. Endpoint: {Endpoint}", endpoint);
                    throw; // Beklenmeyen hataları yeniden fırlat
                }
            }
            
            // Tüm denemeler başarısız
            throw lastException ?? new Exception($"MikroAPI isteği başarısız oldu: {endpoint}");
        }

        // ==================== MEVCUT INTERFACE METODLARİ ====================
        // Bu metodlar geriye dönük uyumluluk için korunuyor ve MikroAPI V2'yi kullanıyor

        /// <summary>
        /// Mikro'dan tüm ürünleri çeker (geriye dönük uyumluluk).
        /// NEDEN: Eski kod bağımlılığı için korunuyor, StokListesiV2 kullanır.
        /// </summary>
        public async Task<IEnumerable<MicroProductDto>> GetProductsAsync()
        {
            _logger.LogInformation("[MicroService] GetProductsAsync çağrılıyor (StokListesiV2 üzerinden)");

            try
            {
                var products = new List<MicroProductDto>();
                
                // Varsayılan depodan tüm stokları çek
                await foreach (var stok in GetAllStokAsync(100, _settings.DefaultDepoNo))
                {
                    products.Add(new MicroProductDto
                    {
                        Sku = stok.StoKod ?? "",
                        Name = stok.StoIsim ?? "",
                        Barcode = stok.Barkod,
                        Price = stok.SatisFiyat1 ?? 0,
                        VatRate = stok.KdvOrani ?? 20,
                        StockQuantity = (int)(stok.MevcutMiktar ?? 0),
                        CategoryCode = stok.GrupKodu,
                        Unit = stok.BirimAdi ?? "ADET",
                        IsActive = stok.Aktif ?? true,
                        LastModified = stok.DegisiklikTarihi
                    });
                }

                _logger.LogInformation(
                    "[MicroService] GetProductsAsync tamamlandı. Toplam: {Count} ürün",
                    products.Count);

                return products;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MicroService] GetProductsAsync hatası.");
                return new List<MicroProductDto>();
            }
        }

        /// <summary>
        /// Mikro'dan tüm stokları çeker (geriye dönük uyumluluk).
        /// NEDEN: Eski kod bağımlılığı için korunuyor, StokListesiV2 kullanır.
        /// </summary>
        public async Task<IEnumerable<MicroStockDto>> GetStocksAsync()
        {
            _logger.LogInformation("[MicroService] GetStocksAsync çağrılıyor (StokListesiV2 üzerinden)");

            try
            {
                var stocks = new List<MicroStockDto>();
                
                // Varsayılan depodan stokları çek
                await foreach (var stok in GetAllStokAsync(100, _settings.DefaultDepoNo))
                {
                    stocks.Add(new MicroStockDto
                    {
                        Sku = stok.StoKod ?? "",
                        Barcode = stok.Barkod,
                        Quantity = (int)(stok.MevcutMiktar ?? 0),
                        AvailableQuantity = (int)(stok.KullanilabilirMiktar ?? stok.MevcutMiktar ?? 0),
                        ReservedQuantity = (int)(stok.RezervedMiktar ?? 0),
                        WarehouseCode = $"DEPO{_settings.DefaultDepoNo}",
                        LastUpdated = stok.DegisiklikTarihi ?? DateTime.UtcNow
                    });
                }

                _logger.LogInformation(
                    "[MicroService] GetStocksAsync tamamlandı. Toplam: {Count} stok",
                    stocks.Count);

                return stocks;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MicroService] GetStocksAsync hatası.");
                return new List<MicroStockDto>();
            }
        }

        public Task<bool> ExportOrdersToERPAsync(IEnumerable<Order> orders)
        {
            // TODO: SiparisKaydetV2 endpoint'i ile değiştirilecek (Adım 3'te)
            _logger.LogWarning("[MicroService] ExportOrdersToERPAsync henüz MikroAPI V2'ye migrate edilmedi.");
            return Task.FromResult(false);
        }

        public void UpdateProduct(MicroProductDto productDto)
        {
            // TODO: StokKaydetV2 endpoint'i ile değiştirilecek (Adım 3'te)
            _logger.LogWarning("[MicroService] UpdateProduct henüz MikroAPI V2'ye migrate edilmedi.");
        }

        public Task<IEnumerable<MicroPriceDto>> GetPricesAsync()
        {
            // TODO: StokListesiV2'den fiyat bilgisi çekilecek (Adım 3'te)
            _logger.LogWarning("[MicroService] GetPricesAsync henüz MikroAPI V2'ye migrate edilmedi.");
            return Task.FromResult<IEnumerable<MicroPriceDto>>(new List<MicroPriceDto>());
        }

        public Task<IEnumerable<MicroCustomerDto>> GetCustomersAsync()
        {
            // TODO: CariListesiV2 endpoint'i ile değiştirilecek (Adım 3'te)
            _logger.LogWarning("[MicroService] GetCustomersAsync henüz MikroAPI V2'ye migrate edilmedi.");
            return Task.FromResult<IEnumerable<MicroCustomerDto>>(new List<MicroCustomerDto>());
        }

        public Task<bool> UpsertProductsAsync(IEnumerable<MicroProductDto> products)
        {
            // TODO: StokKaydetV2 endpoint'i ile değiştirilecek (Adım 3'te)
            _logger.LogWarning("[MicroService] UpsertProductsAsync henüz MikroAPI V2'ye migrate edilmedi.");
            return Task.FromResult(false);
        }

        public Task<bool> UpsertStocksAsync(IEnumerable<MicroStockDto> stocks)
        {
            // TODO: DahiliStokHareketKaydetV2 endpoint'i ile değiştirilecek (Adım 3'te)
            _logger.LogWarning("[MicroService] UpsertStocksAsync henüz MikroAPI V2'ye migrate edilmedi.");
            return Task.FromResult(false);
        }

        public Task<bool> UpsertPricesAsync(IEnumerable<MicroPriceDto> prices)
        {
            // TODO: FiyatDegisikligiKaydetV2 endpoint'i ile değiştirilecek (Adım 3'te)
            _logger.LogWarning("[MicroService] UpsertPricesAsync henüz MikroAPI V2'ye migrate edilmedi.");
            return Task.FromResult(false);
        }

        public async Task<bool> UpsertCustomersAsync(IEnumerable<MicroCustomerDto> customers)
        {
            _logger.LogInformation("[MicroService] UpsertCustomersAsync çağrılıyor (CariKaydetV2 üzerinden)");

            try
            {
                // MikroAPI V2 CariKaydetV2 formatına dönüştür
                var cariler = customers.Select(c => new
                {
                    cari_kod = c.ExternalId,
                    cari_unvan1 = c.FullName,
                    cari_unvan2 = "",
                    cari_EMail = c.Email ?? "",
                    cari_CepTel = c.Phone ?? "",
                    cari_KurHesapSekli = 1,
                    cari_doviz_cinsi1 = 0,
                    cari_doviz_cinsi2 = 255,
                    cari_doviz_cinsi3 = 255,
                    cari_efatura_fl = 0,
                    cari_def_efatura_cinsi = 0,
                    cari_fatura_adres_no = 0,
                    cari_sevk_adres_no = 0,
                    cari_vade_fark_yuz = 0,
                    // Adres bilgisi varsa ekle
                    adres = !string.IsNullOrEmpty(c.Address) ? new[]
                    {
                        new
                        {
                            adr_cadde = c.Address ?? "",
                            adr_il = "",
                            adr_ilce = "",
                            adr_ulke = "TÜRKİYE"
                        }
                    } : Array.Empty<object>()
                }).ToList();

                var requestBody = CreateMikroRequest(new { cariler });

                var response = await SendMikroRequestAsync(
                    "/API/APIMethods/CariKaydetV2",
                    requestBody);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation(
                        "[MicroService] CariKaydetV2 başarılı. Kayıt sayısı: {Count}, Response: {Response}",
                        cariler.Count, responseContent.Length > 200 ? responseContent[..200] + "..." : responseContent);
                    return true;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError(
                        "[MicroService] CariKaydetV2 başarısız. Status: {Status}, Error: {Error}",
                        response.StatusCode, errorContent);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MicroService] UpsertCustomersAsync hatası.");
                return false;
            }
        }

        // ==================== MIKRO API V2 - YENİ METODLAR ====================

        /// <summary>
        /// MikroAPI HealthCheck - Servis durumunu kontrol eder.
        /// Bu metod, API bağlantısının çalışıp çalışmadığını test etmek için kullanılır.
        /// </summary>
        /// <returns>Servis erişilebilirse true, değilse false</returns>
        public async Task<bool> CheckHealthAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _httpClient.GetAsync(
                    "/Api/APIMethods/HealthCheck", 
                    cancellationToken);
                
                var isHealthy = response.IsSuccessStatusCode;
                
                _logger.LogInformation(
                    "[MicroService] HealthCheck sonucu: {IsHealthy}, Status: {Status}",
                    isHealthy, response.StatusCode);
                
                return isHealthy;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MicroService] HealthCheck başarısız.");
                return false;
            }
        }

        /// <summary>
        /// Günlük MD5 hash'in doğruluğunu test eder (debug amaçlı).
        /// Bu metod, şifre hash'inin doğru hesaplanıp hesaplanmadığını kontrol eder.
        /// </summary>
        /// <returns>Hash bilgisi ve tarih</returns>
        public (string Hash, string Date, bool IsConfigured) GetCurrentPasswordHashInfo()
        {
            var today = DateTime.Now.ToString("yyyy-MM-dd");
            var isConfigured = !string.IsNullOrEmpty(_settings.Sifre);
            var hash = isConfigured ? GenerateDailyPasswordHash(_settings.Sifre) : "(şifre ayarlanmamış)";
            
            return (hash, today, isConfigured);
        }

        /// <summary>
        /// Mevcut konfigürasyonun özet bilgisini döndürür (şifre hariç).
        /// Debug ve logging amaçlı kullanılır.
        /// </summary>
        public string GetConfigurationSummary()
        {
            return $"URL: {_settings.ApiUrl}, Firma: {_settings.FirmaKodu}, " +
                   $"Kullanıcı: {_settings.KullaniciKodu}, Yıl: {_settings.CalismaYili}, " +
                   $"Depo: {_settings.DefaultDepoNo}, ApiKey: {(_settings.ApiKey?.Length > 10 ? "***ayarlandı***" : "(boş)")}";
        }

        // ==================== STOK İŞLEMLERİ (V2) ====================

        /// <summary>
        /// MikroAPI V2 StokListesiV2 endpoint'i ile stok/ürün listesi çeker.
        /// 
        /// KULLANIM SENARYOLARI:
        /// 1. Tam senkronizasyon: request = null → Tüm ürünleri çeker
        /// 2. Delta senkronizasyon: DegisiklikTarihiBaslangic set → Sadece değişenleri çeker
        /// 3. Tek depo: DepoNo set → O deponun stoklarını çeker
        /// </summary>
        public async Task<MikroResponseWrapper<MikroStokResponseDto>> GetStokListesiV2Async(
            MikroStokListesiRequestDto? request = null,
            CancellationToken cancellationToken = default)
        {
            const string endpoint = "/Api/APIMethods/StokListesiV2";
            
            try
            {
                _logger.LogInformation(
                    "[MicroService] StokListesiV2 çağrılıyor. Sayfa: {Sayfa}, Depo: {Depo}",
                    request?.SayfaNo ?? 1, request?.DepoNo ?? -1);

                // Request body oluştur
                var requestBody = CreateMikroRequest(request);
                
                _logger.LogInformation(
                    "[MicroService] StokListesiV2 Request: {Request}",
                    JsonSerializer.Serialize(requestBody, new JsonSerializerOptions { WriteIndented = false }));
                
                // API'ye gönder
                var response = await SendMikroRequestAsync(endpoint, requestBody, cancellationToken);
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError(
                        "[MicroService] StokListesiV2 başarısız. Status: {Status}",
                        response.StatusCode);
                    
                    return new MikroResponseWrapper<MikroStokResponseDto>
                    {
                        Success = false,
                        Message = $"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}"
                    };
                }

                // Response'u parse et
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                
                _logger.LogInformation(
                    "[MicroService] StokListesiV2 RAW Response (ilk 500 karakter): {Content}",
                    content.Length > 500 ? content.Substring(0, 500) + "..." : content);
                
                var result = JsonSerializer.Deserialize<MikroResponseWrapper<MikroStokResponseDto>>(
                    content, _jsonOptions);

                _logger.LogInformation(
                    "[MicroService] StokListesiV2 tamamlandı. Success: {Success}, Kayıt: {Count}, Toplam: {Total}, Message: {Message}",
                    result?.Success ?? false, result?.Data?.Count ?? 0, result?.TotalCount ?? 0, result?.Message ?? "null");

                return result ?? new MikroResponseWrapper<MikroStokResponseDto> { Success = false, Message = "Parse hatası" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MicroService] StokListesiV2 hatası.");
                return new MikroResponseWrapper<MikroStokResponseDto>
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        /// <summary>
        /// MikroAPI V2 StokKaydetV2 endpoint'i ile stok/ürün kaydeder.
        /// Yeni ürün ekleme veya mevcut ürün güncelleme için kullanılır.
        /// </summary>
        public async Task<MikroSingleResponseWrapper<MikroStokResponseDto>> SaveStokV2Async(
            MikroStokKaydetRequestDto request,
            CancellationToken cancellationToken = default)
        {
            const string endpoint = "/Api/APIMethods/StokKaydetV2";
            
            try
            {
                _logger.LogInformation(
                    "[MicroService] StokKaydetV2 çağrılıyor. Stok: {StoKod}",
                    request.StoKod);

                var requestBody = CreateMikroRequest(request);
                var response = await SendMikroRequestAsync(endpoint, requestBody, cancellationToken);
                
                if (!response.IsSuccessStatusCode)
                {
                    return new MikroSingleResponseWrapper<MikroStokResponseDto>
                    {
                        Success = false,
                        Message = $"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}"
                    };
                }

                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = JsonSerializer.Deserialize<MikroSingleResponseWrapper<MikroStokResponseDto>>(
                    content, _jsonOptions);

                _logger.LogInformation(
                    "[MicroService] StokKaydetV2 tamamlandı. Stok: {StoKod}, Başarılı: {Success}",
                    request.StoKod, result?.Success ?? false);

                return result ?? new MikroSingleResponseWrapper<MikroStokResponseDto> { Success = false };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MicroService] StokKaydetV2 hatası. Stok: {StoKod}", request.StoKod);
                return new MikroSingleResponseWrapper<MikroStokResponseDto>
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        // ==================== SİPARİŞ İŞLEMLERİ (V2) ====================

        /// <summary>
        /// MikroAPI V2 SiparisKaydetV2 endpoint'i ile sipariş kaydeder.
        /// E-ticaret siparişlerini Mikro ERP'ye aktarmak için kullanılır.
        /// 
        /// ÖNEMLİ: Sipariş kaydedilmeden önce müşteri (cari) kontrolü yapılmalı.
        /// Yeni müşteriyse önce SaveCariV2Async ile cari hesap oluşturulmalı.
        /// </summary>
        public async Task<MikroSingleResponseWrapper<MikroSiparisKaydetResponseDto>> SaveSiparisV2Async(
            MikroSiparisKaydetRequestDto request,
            CancellationToken cancellationToken = default)
        {
            const string endpoint = "/Api/APIMethods/SiparisKaydetV2";
            
            try
            {
                _logger.LogInformation(
                    "[MicroService] SiparisKaydetV2 çağrılıyor. Müşteri: {Musteri}, Kalem: {Kalem}",
                    request.SipMusteriKod, request.Satirlar?.Count ?? 0);

                var requestBody = CreateMikroRequest(request);
                var response = await SendMikroRequestAsync(endpoint, requestBody, cancellationToken);
                
                if (!response.IsSuccessStatusCode)
                {
                    return new MikroSingleResponseWrapper<MikroSiparisKaydetResponseDto>
                    {
                        Success = false,
                        Message = $"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}"
                    };
                }

                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = JsonSerializer.Deserialize<MikroSingleResponseWrapper<MikroSiparisKaydetResponseDto>>(
                    content, _jsonOptions);

                _logger.LogInformation(
                    "[MicroService] SiparisKaydetV2 tamamlandı. Evrak: {Seri}-{Sira}, Başarılı: {Success}",
                    result?.Data?.EvrakSeri, result?.Data?.EvrakSira, result?.Success ?? false);

                return result ?? new MikroSingleResponseWrapper<MikroSiparisKaydetResponseDto> { Success = false };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MicroService] SiparisKaydetV2 hatası.");
                return new MikroSingleResponseWrapper<MikroSiparisKaydetResponseDto>
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        /// <summary>
        /// MikroAPI V2 SiparisListesiV2 endpoint'i ile sipariş listesi çeker.
        /// Mağazadan verilen siparişleri veya durum güncellemelerini takip etmek için.
        /// </summary>
        public async Task<MikroResponseWrapper<MikroSiparisListesiResponseDto>> GetSiparisListesiV2Async(
            MikroSiparisListesiRequestDto? request = null,
            CancellationToken cancellationToken = default)
        {
            const string endpoint = "/Api/APIMethods/SiparisListesiV2";
            
            try
            {
                _logger.LogInformation(
                    "[MicroService] SiparisListesiV2 çağrılıyor. Başlangıç: {Baslangic}",
                    request?.BaslangicTarih ?? "Belirtilmedi");

                var requestBody = CreateMikroRequest(request);
                var response = await SendMikroRequestAsync(endpoint, requestBody, cancellationToken);
                
                if (!response.IsSuccessStatusCode)
                {
                    return new MikroResponseWrapper<MikroSiparisListesiResponseDto>
                    {
                        Success = false,
                        Message = $"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}"
                    };
                }

                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = JsonSerializer.Deserialize<MikroResponseWrapper<MikroSiparisListesiResponseDto>>(
                    content, _jsonOptions);

                _logger.LogInformation(
                    "[MicroService] SiparisListesiV2 tamamlandı. Sipariş: {Count}",
                    result?.Data?.Count ?? 0);

                return result ?? new MikroResponseWrapper<MikroSiparisListesiResponseDto> { Success = false };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MicroService] SiparisListesiV2 hatası.");
                return new MikroResponseWrapper<MikroSiparisListesiResponseDto>
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        // ==================== CARİ İŞLEMLERİ (V2) ====================

        /// <summary>
        /// MikroAPI V2 CariKaydetV2 endpoint'i ile müşteri (cari) kaydeder.
        /// Yeni müşteri kayıt veya mevcut müşteri güncellemesi için kullanılır.
        /// 
        /// KULLANIM: Sipariş kaydetmeden önce müşteri var mı kontrol edilmeli,
        /// yoksa bu metod ile önce cari hesap oluşturulmalı.
        /// </summary>
        public async Task<MikroSingleResponseWrapper<MikroCariKaydetResponseDto>> SaveCariV2Async(
            MikroCariKaydetRequestDto request,
            CancellationToken cancellationToken = default)
        {
            const string endpoint = "/Api/APIMethods/CariKaydetV2";
            
            try
            {
                _logger.LogInformation(
                    "[MicroService] CariKaydetV2 çağrılıyor. Cari: {CariKod}, Ünvan: {Unvan}",
                    request.CariKod, request.CariUnvan1);

                var requestBody = CreateMikroRequest(request);
                var response = await SendMikroRequestAsync(endpoint, requestBody, cancellationToken);
                
                if (!response.IsSuccessStatusCode)
                {
                    return new MikroSingleResponseWrapper<MikroCariKaydetResponseDto>
                    {
                        Success = false,
                        Message = $"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}"
                    };
                }

                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = JsonSerializer.Deserialize<MikroSingleResponseWrapper<MikroCariKaydetResponseDto>>(
                    content, _jsonOptions);

                _logger.LogInformation(
                    "[MicroService] CariKaydetV2 tamamlandı. Cari: {CariKod}, Yeni: {YeniKayit}",
                    result?.Data?.CariKod, result?.Data?.YeniKayit ?? false);

                return result ?? new MikroSingleResponseWrapper<MikroCariKaydetResponseDto> { Success = false };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MicroService] CariKaydetV2 hatası. Cari: {CariKod}", request.CariKod);
                return new MikroSingleResponseWrapper<MikroCariKaydetResponseDto>
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        /// <summary>
        /// MikroAPI V2 CariListesiV2 endpoint'i ile müşteri listesi çeker.
        /// </summary>
        public async Task<MikroResponseWrapper<MikroCariResponseDto>> GetCariListesiV2Async(
            MikroCariListesiRequestDto? request = null,
            CancellationToken cancellationToken = default)
        {
            const string endpoint = "/Api/APIMethods/CariListesiV2";
            
            try
            {
                _logger.LogInformation("[MicroService] CariListesiV2 çağrılıyor.");

                var requestBody = CreateMikroRequest(request);
                var response = await SendMikroRequestAsync(endpoint, requestBody, cancellationToken);
                
                if (!response.IsSuccessStatusCode)
                {
                    return new MikroResponseWrapper<MikroCariResponseDto>
                    {
                        Success = false,
                        Message = $"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}"
                    };
                }

                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = JsonSerializer.Deserialize<MikroResponseWrapper<MikroCariResponseDto>>(
                    content, _jsonOptions);

                _logger.LogInformation(
                    "[MicroService] CariListesiV2 tamamlandı. Cari: {Count}",
                    result?.Data?.Count ?? 0);

                return result ?? new MikroResponseWrapper<MikroCariResponseDto> { Success = false };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MicroService] CariListesiV2 hatası.");
                return new MikroResponseWrapper<MikroCariResponseDto>
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        // ==================== FİYAT İŞLEMLERİ (V2) ====================

        /// <summary>
        /// MikroAPI V2 FiyatListesiV2 endpoint'i ile fiyat listesi çeker.
        /// Delta senkronizasyonda değişen fiyatları tespit etmek için kullanılır.
        /// </summary>
        public async Task<MikroResponseWrapper<MikroFiyatListesiResponseDto>> GetFiyatListesiV2Async(
            MikroFiyatListesiRequestDto? request = null,
            CancellationToken cancellationToken = default)
        {
            const string endpoint = "/Api/APIMethods/FiyatListesiV2";
            
            try
            {
                _logger.LogInformation(
                    "[MicroService] FiyatListesiV2 çağrılıyor. FiyatNo: {FiyatNo}",
                    request?.FiyatNo ?? -1);

                var requestBody = CreateMikroRequest(request);
                var response = await SendMikroRequestAsync(endpoint, requestBody, cancellationToken);
                
                if (!response.IsSuccessStatusCode)
                {
                    return new MikroResponseWrapper<MikroFiyatListesiResponseDto>
                    {
                        Success = false,
                        Message = $"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}"
                    };
                }

                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = JsonSerializer.Deserialize<MikroResponseWrapper<MikroFiyatListesiResponseDto>>(
                    content, _jsonOptions);

                _logger.LogInformation(
                    "[MicroService] FiyatListesiV2 tamamlandı. Fiyat: {Count}",
                    result?.Data?.Count ?? 0);

                return result ?? new MikroResponseWrapper<MikroFiyatListesiResponseDto> { Success = false };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MicroService] FiyatListesiV2 hatası.");
                return new MikroResponseWrapper<MikroFiyatListesiResponseDto>
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        /// <summary>
        /// MikroAPI V2 FiyatDegisikligiKaydetV2 endpoint'i ile fiyat günceller.
        /// </summary>
        public async Task<MikroSingleResponseWrapper<MikroFiyatGuncellemeResponseDto>> SaveFiyatDegisikligiV2Async(
            MikroFiyatDegisikligiRequestDto request,
            CancellationToken cancellationToken = default)
        {
            const string endpoint = "/Api/APIMethods/FiyatDegisikligiKaydetV2";
            
            try
            {
                _logger.LogInformation(
                    "[MicroService] FiyatDegisikligiKaydetV2 çağrılıyor. Stok: {StoKod}, Fiyat: {Fiyat}",
                    request.StoKod, request.YeniFiyat);

                var requestBody = CreateMikroRequest(request);
                var response = await SendMikroRequestAsync(endpoint, requestBody, cancellationToken);
                
                if (!response.IsSuccessStatusCode)
                {
                    return new MikroSingleResponseWrapper<MikroFiyatGuncellemeResponseDto>
                    {
                        Success = false,
                        Message = $"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}"
                    };
                }

                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = JsonSerializer.Deserialize<MikroSingleResponseWrapper<MikroFiyatGuncellemeResponseDto>>(
                    content, _jsonOptions);

                _logger.LogInformation(
                    "[MicroService] FiyatDegisikligiKaydetV2 tamamlandı. Stok: {StoKod}",
                    request.StoKod);

                return result ?? new MikroSingleResponseWrapper<MikroFiyatGuncellemeResponseDto> { Success = false };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MicroService] FiyatDegisikligiKaydetV2 hatası. Stok: {StoKod}", request.StoKod);
                return new MikroSingleResponseWrapper<MikroFiyatGuncellemeResponseDto>
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        // ==================== TOPLU İŞLEMLER (V2) ====================

        /// <summary>
        /// Tüm stokları sayfalı olarak çeker (full sync).
        /// NEDEN: İlk kurulumda veya tam senkronizasyon gerektiğinde
        /// tüm ürünlerin çekilmesi için kullanılır.
        /// </summary>
        public async IAsyncEnumerable<MikroStokResponseDto> GetAllStokAsync(
            int pageSize = 100,
            int? depoNo = null,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            int pageNo = 1;
            bool hasMore = true;

            _logger.LogInformation(
                "[MicroService] Tam stok senkronizasyonu başlıyor. Depo: {Depo}",
                depoNo ?? -1);

            while (hasMore && !cancellationToken.IsCancellationRequested)
            {
                var request = new MikroStokListesiRequestDto
                {
                    SayfaNo = pageNo,
                    SayfaBuyuklugu = pageSize,
                    DepoNo = depoNo ?? _settings.DefaultDepoNo,
                    FiyatDahil = true,
                    BarkodDahil = true
                };

                var result = await GetStokListesiV2Async(request, cancellationToken);

                if (!result.Success || result.Data == null || result.Data.Count == 0)
                {
                    hasMore = false;
                    continue;
                }

                foreach (var stok in result.Data)
                {
                    yield return stok;
                }

                // Sonraki sayfa var mı kontrol et
                hasMore = result.Data.Count == pageSize;
                pageNo++;

                _logger.LogDebug(
                    "[MicroService] Sayfa {Page} tamamlandı. Kayıt: {Count}",
                    pageNo - 1, result.Data.Count);
            }

            _logger.LogInformation("[MicroService] Tam stok senkronizasyonu tamamlandı.");
        }

        // ==================== FATURA İŞLEMLERİ (V2) ====================

        /// <summary>
        /// MikroAPI V2 FaturaKaydetV2 endpoint'i ile fatura kaydeder.
        /// 
        /// ÖNEMLİ: Bu metod çağrıldığında:
        /// - Stok otomatik düşer (sth_cikis_depo_no'dan)
        /// - Cari hesaba borç yazılır
        /// - E-arşiv zorunluysa e-arşiv fatura kesilir
        /// 
        /// KULLANIM: E-ticaret siparişi tamamlandığında fatura kesmek için.
        /// </summary>
        public async Task<MikroSingleResponseWrapper<MikroFaturaKaydetResponseDto>> SaveFaturaV2Async(
            MikroFaturaKaydetRequestDto request,
            CancellationToken cancellationToken = default)
        {
            const string endpoint = "/api/APIMethods/FaturaKaydetV2";
            
            try
            {
                var evrakCount = request.Evraklar?.Count ?? 0;
                var satirCount = request.Evraklar?.Sum(e => e.Detay?.Count ?? 0) ?? 0;

                _logger.LogInformation(
                    "[MicroService] FaturaKaydetV2 çağrılıyor. Evrak: {Evrak}, Satır: {Satir}",
                    evrakCount, satirCount);

                var requestBody = CreateMikroRequest(request);
                var response = await SendMikroRequestAsync(endpoint, requestBody, cancellationToken);
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError(
                        "[MicroService] FaturaKaydetV2 HTTP hatası. Status: {Status}",
                        response.StatusCode);

                    return new MikroSingleResponseWrapper<MikroFaturaKaydetResponseDto>
                    {
                        Success = false,
                        Message = $"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}"
                    };
                }

                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = JsonSerializer.Deserialize<MikroSingleResponseWrapper<MikroFaturaKaydetResponseDto>>(
                    content, _jsonOptions);

                if (result?.Success == true)
                {
                    _logger.LogInformation(
                        "[MicroService] FaturaKaydetV2 başarılı. Evrak: {Seri}-{Sira}, E-Arşiv: {EArsiv}",
                        result.Data?.EvrakSeri, result.Data?.EvrakSira, result.Data?.EArsivNo ?? "(yok)");
                }
                else
                {
                    _logger.LogWarning(
                        "[MicroService] FaturaKaydetV2 başarısız. Mesaj: {Message}",
                        result?.Message ?? content);
                }

                return result ?? new MikroSingleResponseWrapper<MikroFaturaKaydetResponseDto> 
                { 
                    Success = false, 
                    Message = "Response parse hatası" 
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MicroService] FaturaKaydetV2 exception hatası.");
                return new MikroSingleResponseWrapper<MikroFaturaKaydetResponseDto>
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        /// <summary>
        /// Siparişten fatura oluşturma (kısayol metod).
        /// NEDEN: E-ticaret siparişinden doğrudan fatura kesmek için utility metod.
        /// 
        /// Bu metod:
        /// 1. Sipariş verisini alır
        /// 2. MikroFaturaKaydetRequestDto'ya dönüştürür
        /// 3. SaveFaturaV2Async'i çağırır
        /// </summary>
        public async Task<MikroSingleResponseWrapper<MikroFaturaKaydetResponseDto>> CreateInvoiceFromOrderAsync(
            string cariKod,
            string siparisNo,
            decimal araToplam,
            List<(string StokKod, decimal Miktar, decimal BirimFiyat, decimal KdvTutari)> kalemler,
            string? musteriEmail = null,
            string? musteriAd = null,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
                "[MicroService] Siparişten fatura oluşturuluyor. Sipariş: {SiparisNo}, Cari: {Cari}",
                siparisNo, cariKod);

            // Fatura DTO oluştur
            var faturaRequest = new MikroFaturaKaydetRequestDto
            {
                Evraklar = new List<MikroFaturaEvrakDto>
                {
                    new MikroFaturaEvrakDto
                    {
                        ChaEvraknoSeri = _settings.DefaultEvrakSeri,
                        ChaTarihi = DateTime.Now.ToString("dd.MM.yyyy"),
                        ChaKod = cariKod,
                        ChaTip = 0, // Satış
                        ChaCinsi = 8, // Perakende Satış Faturası
                        ChaDCins = 0, // TL
                        ChaDKur = 1,
                        ChaAratoplam = araToplam,
                        ChaAciklama = $"E-ticaret sipariş: {siparisNo}",
                        ChaVade = 0, // Peşin
                        ChaEvrakTip = 63,
                        ChaEArsivMail = musteriEmail ?? "",
                        ChaEArsivUnvaniAd = musteriAd ?? "",
                        Detay = kalemler.Select((k, i) => new MikroFaturaSatirDto
                        {
                            SthStokKod = k.StokKod,
                            SthMiktar = k.Miktar,
                            SthTutar = k.BirimFiyat * k.Miktar,
                            SthVergi = k.KdvTutari,
                            SthEvraknoSeri = _settings.DefaultEvrakSeri,
                            SthEvraktip = 4, // Satış Faturası
                            SthTip = 1, // Satış
                            SthCikisDepoNo = _settings.DefaultDepoNo, // VARSAYILAN DEPO
                            SthGirisDepoNo = _settings.DefaultDepoNo,
                            SthCariKodu = cariKod,
                            SthTarih = DateTime.Now.ToString("dd.MM.yyyy"),
                            SthAciklama = $"Sipariş: {siparisNo}",
                            UserTablo = new List<MikroFaturaUserTabloDto>
                            {
                                new MikroFaturaUserTabloDto
                                {
                                    EticaretOrderId = siparisNo,
                                    Aciklama = $"E-ticaret satır {i + 1}"
                                }
                            }
                        }).ToList(),
                        EbelgeDetay = new List<MikroFaturaEbelgeDetayDto>
                        {
                            new MikroFaturaEbelgeDetayDto
                            {
                                EbhOdemeSekli = 1, // Kredi Kartı (e-ticaret için varsayılan)
                                EbhSatisinWebadresi = "https://www.eticaret.com" // Config'den alınabilir
                            }
                        },
                        UserTablo = new List<MikroFaturaUserTabloDto>
                        {
                            new MikroFaturaUserTabloDto
                            {
                                EticaretOrderId = siparisNo
                            }
                        }
                    }
                }
            };

            return await SaveFaturaV2Async(faturaRequest, cancellationToken);
        }
    }

    // ==================== MIKRO AUTH WRAPPER DTO ====================
    
    /// <summary>
    /// MikroAPI V2 için authentication wrapper sınıfı.
    /// Tüm V2 endpoint'leri request body'de bu yapıyı bekler.
    /// Sadece 5 alan içermelidir: ApiKey, CalismaYili, FirmaKodu, KullaniciKodu, Sifre
    /// </summary>
    internal class MikroAuthWrapper
    {
        /// <summary>MikroAPI anahtarı</summary>
        public string ApiKey { get; set; } = string.Empty;
        
        /// <summary>Çalışma yılı (mali yıl)</summary>
        public string CalismaYili { get; set; } = string.Empty;
        
        /// <summary>Firma kodu</summary>
        public string FirmaKodu { get; set; } = string.Empty;
        
        /// <summary>Kullanıcı kodu (genellikle "SRV")</summary>
        public string KullaniciKodu { get; set; } = string.Empty;
        
        /// <summary>Günlük MD5 hash'lenmiş şifre</summary>
        public string Sifre { get; set; } = string.Empty;
    }
}
