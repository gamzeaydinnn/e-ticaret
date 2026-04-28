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
using ECommerce.Infrastructure.Resilience;
using ECommerce.Core.Exceptions;
using System.Text.RegularExpressions;
using System.Globalization;
using Polly.CircuitBreaker;

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
        // Direkt SQL bağlantı servisi — HTTP SqlVeriOkuV2 timeout sorununu ortadan kaldırır
        private readonly IMikroDbService _mikroDbService;
        // Polly resilience pipeline — circuit breaker + retry + timeout
        private readonly MikroResiliencePipelineFactory _resilienceFactory;
        private static readonly Regex Md5Regex = new("^[a-fA-F0-9]{32}$", RegexOptions.Compiled);
        private static readonly SemaphoreSlim StockSnapshotLock = new(1, 1);
        private static List<MikroStokResponseDto>? _sharedStockSnapshot;
        private static DateTime _sharedStockSnapshotAtUtc;
        private static int _sharedStockSnapshotDepoNo = int.MinValue;
        private static readonly TimeSpan StockSnapshotCacheTtl = TimeSpan.FromSeconds(20);
        private static readonly SemaphoreSlim SqlPriceMapLock = new(1, 1);
        private static Dictionary<string, MikroFiyatSatirDto>? _sharedSqlPriceMap;
        private static DateTime _sharedSqlPriceMapAtUtc;
        private static int _sharedSqlPriceMapListNo = int.MinValue;
        private static readonly TimeSpan SqlPriceMapCacheTtl = TimeSpan.FromSeconds(20);
        private static int _initLogWritten;
        private const string StokListesiV2Endpoint = "/Api/APIMethods/StokListesiV2";
        private const string SqlVeriOkuV2Endpoint = "/Api/APIMethods/SqlVeriOkuV2";
        private static readonly TimeSpan StokListesiTimeout = TimeSpan.FromSeconds(45);
        private static readonly TimeSpan StokReadCooldown = TimeSpan.FromMinutes(3);
        private const int StokTimeoutStrikeThreshold = 2;
        private static readonly TimeSpan SqlVeriOkuTimeout = TimeSpan.FromSeconds(180);
        private static readonly TimeSpan SqlReadCooldown = TimeSpan.FromMinutes(2);
        private const int SqlTimeoutStrikeThreshold = 2;
        private static readonly object CircuitStateLock = new();
        private static DateTime _skipStokReadsUntilUtc = DateTime.MinValue;
        private static int _stokTimeoutStrike;
        private static DateTime _skipSqlReadsUntilUtc = DateTime.MinValue;
        private static int _sqlTimeoutStrike;
        
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
            ILogger<MicroService> logger,
            IMikroDbService mikroDbService,
            MikroResiliencePipelineFactory resilienceFactory)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            // Direkt DB servisi — timeout sorunu bu injection ile çözülüyor
            _mikroDbService = mikroDbService ?? throw new ArgumentNullException(nameof(mikroDbService));
            // Polly resilience pipeline — circuit breaker + retry + timeout katmanları
            _resilienceFactory = resilienceFactory ?? throw new ArgumentNullException(nameof(resilienceFactory));
            
            // HttpClient timeout ayarı
            var minTimeoutForSql = (int)Math.Ceiling(SqlVeriOkuTimeout.TotalSeconds) + 10;
            var effectiveTimeoutSeconds = Math.Max(_settings.RequestTimeoutSeconds, minTimeoutForSql);
            _httpClient.Timeout = TimeSpan.FromSeconds(effectiveTimeoutSeconds);
            
            if (Interlocked.Exchange(ref _initLogWritten, 1) == 0)
            {
                _logger.LogInformation(
                    "[MicroService] Başlatıldı. API URL: {ApiUrl}, Firma: {FirmaKodu}, Yıl: {CalismaYili}",
                    _settings.ApiUrl, _settings.FirmaKodu, _settings.CalismaYili);
            }
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
            // Şifre boş olsa bile Mikro formatı gereği "yyyy-MM-dd " değeri hash'lenir.
            plainPassword ??= string.Empty;

            // MikroAPI formatı: YYYY-MM-DD + boşluk + şifre
            var today = DateTime.Now.ToString("yyyy-MM-dd");
            var dataToHash = today + " " + plainPassword;  // ✅ Boşluk karakteri eklendi
            
            // MD5 hash hesapla
            using var md5 = MD5.Create();
            var inputBytes = Encoding.UTF8.GetBytes(dataToHash);
            var hashBytes = md5.ComputeHash(inputBytes);
            
            // Hex string'e çevir (lowercase)
            var hashString = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            
            _logger.LogDebug("[MicroService] MD5 hash oluşturuldu. Tarih: {Date}, SifreBosMu: {IsEmpty}", today, string.IsNullOrEmpty(plainPassword));
            
            return hashString;
        }

        /// <summary>
        /// Sifre alanini MikroAPI'ye gonderilecek son forma cevirir.
        /// - PasswordIsPreHashed=true ise oldugu gibi kullanir
        /// - 32 karakter hex formatinda ise hash kabul eder
        /// - Diger durumlarda gunluk MD5 hash uretir
        /// </summary>
        private string ResolveOutgoingPassword()
        {
            var password = _settings.Sifre;
            if (password == null) return string.Empty;

            if (_settings.PasswordIsPreHashed || Md5Regex.IsMatch(password))
            {
                _logger.LogInformation("[MicroService] Mikro sifresi pre-hashed olarak kullaniliyor.");
                return password.ToLowerInvariant();
            }

            var plainPassword = string.IsNullOrWhiteSpace(password) ? string.Empty : password;
            return GenerateDailyPasswordHash(plainPassword);
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
                Sifre = ResolveOutgoingPassword(),
                FirmaNo = _settings.DefaultFirmaNo,
                SubeNo = _settings.DefaultSubeNo
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
        /// MikroAPI'ye POST isteği gönderir — Polly resilience pipeline üzerinden.
        /// 
        /// RESILIENCE KATMANLARI:
        /// 1. Total Timeout → tüm retry'lar dahil max süre
        /// 2. Retry → exponential backoff + jitter (5xx, timeout, network hataları)
        /// 3. Circuit Breaker → ardışık hatalarda devreyi açar
        /// 4. Per-Attempt Timeout → tek istek zaman aşımı
        /// 
        /// Circuit breaker açıksa MikroCircuitOpenException fırlatılır.
        /// </summary>
        private async Task<HttpResponseMessage> SendMikroRequestAsync(
            string endpoint, 
            object requestBody,
            CancellationToken cancellationToken = default,
            int? maxAttemptsOverride = null)
        {
            _logger.LogDebug(
                "[MicroService] İstek gönderiliyor. Endpoint: {Endpoint}",
                endpoint);

            // Request body'yi JSON'a serialize et (pipeline dışında — her retry'da yeni content gerekir)
            var jsonContent = JsonSerializer.Serialize(requestBody, _jsonOptions);

            try
            {
                // Polly pipeline ile istek gönder — retry, CB, timeout otomatik yönetilir
                var pipeline = _resilienceFactory.GetHttpPipeline();
                var response = await pipeline.ExecuteAsync(async ct =>
                {
                    // Her retry denemesinde yeni StringContent oluşturulmalı (HttpContent tek kullanımlık)
                    var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                    return await _httpClient.PostAsync(endpoint, content, ct);
                }, cancellationToken);

                _logger.LogInformation(
                    "[MicroService] İstek tamamlandı. Endpoint: {Endpoint}, Status: {Status}",
                    endpoint, response.StatusCode);

                return response;
            }
            catch (BrokenCircuitException ex)
            {
                // Circuit breaker açık — Mikro API erişilemez durumda
                _logger.LogError(
                    "[MicroService] Circuit Breaker AÇIK — Mikro API istekleri engellendi. Endpoint: {Endpoint}",
                    endpoint);

                throw new MikroCircuitOpenException(
                    $"Mikro API circuit breaker açık. Endpoint: {endpoint}",
                    _resilienceFactory.HttpCircuitState == Resilience.CircuitBreakerState.Open
                        ? TimeSpan.FromSeconds(30) : null)
                {
                    StokKod = null,
                    Direction = "FromERP"
                };
            }
            catch (Polly.Timeout.TimeoutRejectedException ex)
            {
                _logger.LogError(ex,
                    "[MicroService] Toplam timeout aşıldı. Endpoint: {Endpoint}", endpoint);

                throw new MikroSyncTimeoutException(
                    $"Mikro API timeout: {endpoint}",
                    TimeSpan.FromSeconds(_settings.RequestTimeoutSeconds),
                    ex)
                {
                    Direction = "FromERP"
                };
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex,
                    "[MicroService] Tüm retry denemeleri başarısız. Endpoint: {Endpoint}", endpoint);

                throw new MikroApiException(
                    $"Mikro API isteği başarısız: {endpoint} — {ex.Message}",
                    statusCode: null,
                    inner: ex)
                {
                    Endpoint = endpoint,
                    Direction = "FromERP"
                };
            }
            catch (MikroSyncException)
            {
                throw; // Domain exception'ları olduğu gibi geçir
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[MicroService] Beklenmeyen hata. Endpoint: {Endpoint}", endpoint);
                throw;
            }
        }

        // ==================== MEVCUT INTERFACE METODLARİ ====================
        // Bu metodlar geriye dönük uyumluluk için korunuyor ve MikroAPI V2'yi kullanıyor

        /// <summary>
        /// [LEGACY] Kısa süreli stok snapshot'ı döndürür.
        /// Aynı anda gelen products/stocks isteklerinde Mikro'ya tekrar full tarama gönderilmesini engeller.
        /// NEDEN DEPRECATED: Birleşik SQL sorgusu (GetUnifiedProductMapAsync) ile StokListesiV2 gereksizleşti.
        /// </summary>
        [Obsolete("Birleşik SQL akışı (GetUnifiedProductMapAsync) kullanın. Bu metod legacy StokListesiV2 akışı içindir.")]
        private async Task<List<MikroStokResponseDto>> GetStockSnapshotAsync(CancellationToken cancellationToken = default)
        {
            await StockSnapshotLock.WaitAsync(cancellationToken);
            try
            {
                var now = DateTime.UtcNow;
                var targetDepoNo = _settings.DefaultDepoNo;
                var staleSnapshot = _sharedStockSnapshot;
                if (_sharedStockSnapshot != null &&
                    _sharedStockSnapshotDepoNo == targetDepoNo &&
                    (now - _sharedStockSnapshotAtUtc) < StockSnapshotCacheTtl)
                {
                    _logger.LogInformation(
                        "[MicroService] Stock snapshot cache kullanılıyor. Depo: {DepoNo}, Yaş: {AgeSeconds}s, Kayıt: {Count}",
                        targetDepoNo,
                        (int)(now - _sharedStockSnapshotAtUtc).TotalSeconds,
                        _sharedStockSnapshot.Count);

                    return new List<MikroStokResponseDto>(_sharedStockSnapshot);
                }

                _logger.LogInformation("[MicroService] Stock snapshot yenileniyor. Depo: {DepoNo}", targetDepoNo);

                var allStoks = await GetAllStokParallelAsync(500, targetDepoNo, cancellationToken);

                if (allStoks.Count == 0 &&
                    staleSnapshot != null &&
                    staleSnapshot.Count > 0 &&
                    _sharedStockSnapshotDepoNo == targetDepoNo)
                {
                    _logger.LogWarning(
                        "[MicroService] Stock snapshot yenilemesi boş döndü. Son bilinen snapshot kullanılacak. Depo: {DepoNo}, Kayıt: {Count}",
                        targetDepoNo,
                        staleSnapshot.Count);

                    return new List<MikroStokResponseDto>(staleSnapshot);
                }

                _sharedStockSnapshot = allStoks;
                _sharedStockSnapshotAtUtc = DateTime.UtcNow;
                _sharedStockSnapshotDepoNo = targetDepoNo;

                return new List<MikroStokResponseDto>(allStoks);
            }
            finally
            {
                StockSnapshotLock.Release();
            }
        }

        /// <summary>
        /// Mikro ERP'den ürünleri doğrudan SQL üzerinden çeker.
        /// YÖNTEM: GetUnifiedProductMapAsync → STOKLAR + STOK_HAREKETTEN_ELDEKI_MIKTAR_VIEW + STOK_SATIS_FIYAT_LISTELERI
        /// HTTP API çağrısı yapılmaz; timeout sorunu yoktur.
        /// </summary>
        public async Task<IEnumerable<MicroProductDto>> GetProductsAsync()
        {
            _logger.LogInformation("[MicroService] GetProductsAsync çağrılıyor (SQL tabanlı birleşik sorgu)");

            try
            {
                var unified = await GetUnifiedProductMapAsync();

                var products = unified.Select(u => new MicroProductDto
                {
                    Sku = u.StokKod,
                    Name = u.StokAd,
                    Barcode = u.Barkod,
                    Price = u.Fiyat,
                    VatRate = u.KdvOrani,
                    StockQuantity = (int)Math.Max(0, u.StokMiktar),
                    Stock = (int)Math.Max(0, u.StokMiktar),
                    // NEDEN: AnagrupKod = ana kategori kodu (doğru). GrupKod = alt grup (yanlıştı).
                    // Kategori eşleme AnagrupKod'a göre yapılmalı, GrupKod yedek bilgi.
                    CategoryCode = !string.IsNullOrWhiteSpace(u.AnagrupKod) ? u.AnagrupKod : u.GrupKod,
                    Unit = string.IsNullOrWhiteSpace(u.Birim) ? "ADET" : u.Birim,
                    IsActive = u.WebeGonderilecekFl,
                    LastModified = u.SonHareketTarihi
                }).ToList();

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
        /// [LEGACY] Mikro'dan tüm stokları çeker (geriye dönük uyumluluk).
        /// NEDEN DEPRECATED: Birleşik SQL akışı ile stok verisi tek sorguda gelir.
        /// Eski StokSyncService bağımlılığı için korunuyor.
        /// </summary>
        [Obsolete("Birleşik SQL akışı (GetUnifiedProductMapAsync) kullanın. Bu metod legacy StokListesiV2 akışı içindir.")]
        public async Task<IEnumerable<MicroStockDto>> GetStocksAsync()
        {
            _logger.LogInformation("[MicroService] GetStocksAsync çağrılıyor (Sıralı StokListesiV2 üzerinden)");

            try
            {
                var allStoks = await GetStockSnapshotAsync();
                
                var stocks = allStoks.Select(stok => new MicroStockDto
                {
                    Sku = stok.StoKod ?? "",
                    Barcode = stok.Barkod,
                    Quantity = ResolveStockQuantityFromStok(stok),
                    Stock = ResolveStockQuantityFromStok(stok),
                    AvailableQuantity = ResolveAvailableQuantityFromStok(stok),
                    ReservedQuantity = (int)(stok.RezervedMiktar ?? 0),
                    WarehouseCode = $"DEPO{_settings.DefaultDepoNo}",
                    LastUpdated = stok.DegisiklikTarihi ?? DateTime.UtcNow
                }).ToList();

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

        private static decimal ResolveProductPriceFromStok(MikroStokResponseDto stok)
        {
            if (stok.SatisFiyat1.HasValue && stok.SatisFiyat1.Value > 0)
            {
                return stok.SatisFiyat1.Value;
            }

            var fallback = stok.SatisFiyatlari?
                .OrderBy(f => f.SfiyatNo)
                .FirstOrDefault(f => f.SfiyatFiyati > 0);

            if (fallback?.SfiyatFiyati > 0)
            {
                return fallback.SfiyatFiyati;
            }

            // Bazı kayıtlarda satış fiyatı boş gelirken son alış değeri dolu olabiliyor.
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

        /// <summary>
        /// Mikro stok verisinden en doğru stok miktarını çözer.
        /// 
        /// NEDEN: MikroAPI farklı ortamlarda farklı alanları doldurabilir.
        /// Bu metod tüm olasılıkları kontrol ederek en güvenilir değeri bulur.
        /// 
        /// ÖNCELİK SIRASI (en güvenilirden az güvenilire):
        /// 1. SatilabilirMiktar (depo bazlı, rezerve düşülmüş)
        /// 2. KullanilabilirMiktar (hesaplanmış)
        /// 3. DepoMiktar (belirli depo stoğu)
        /// 4. StoMiktar (toplam stok)
        /// 5. DepoStoklari toplamı (çoklu depo)
        /// </summary>
        private static int ResolveStockQuantityFromStok(MikroStokResponseDto stok, int? targetDepoNo = null)
        {
            // 1. Belirli bir depo istendiyse, önce DepoStoklari'ndan o depoyu ara
            if (targetDepoNo.HasValue && targetDepoNo.Value > 0 && stok.DepoStoklari?.Any() == true)
            {
                var depoStok = stok.DepoStoklari.FirstOrDefault(d => d.DepNo == targetDepoNo.Value);
                if (depoStok != null)
                {
                    // Satılabilir miktar varsa onu al, yoksa stok miktarını
                    var depoMiktar = depoStok.SatilabilirMiktar ?? depoStok.StokMiktar;
                    if (depoMiktar > 0)
                    {
                        return (int)Math.Max(0, Math.Floor(depoMiktar));
                    }
                }
            }

            // 2. Tüm depoların toplamı isteniyorsa (targetDepoNo = 0 veya null)
            if ((!targetDepoNo.HasValue || targetDepoNo.Value == 0) && stok.DepoStoklari?.Any() == true)
            {
                var toplamDepoStok = stok.DepoStoklari.Sum(d => d.SatilabilirMiktar ?? d.StokMiktar);
                if (toplamDepoStok > 0)
                {
                    return (int)Math.Max(0, Math.Floor(toplamDepoStok));
                }
            }

            // 3. Diğer alanlardan en yüksek değeri bul
            var candidates = new List<decimal>();
            
            // KullanilabilirMiktar: DepoMiktar - Rezerve hesabı (en güvenilir)
            if (stok.KullanilabilirMiktar.HasValue && stok.KullanilabilirMiktar.Value > 0)
            {
                candidates.Add(stok.KullanilabilirMiktar.Value);
            }
            
            // DepoMiktar: API'den gelen depo stoğu
            if (stok.DepoMiktar.HasValue && stok.DepoMiktar.Value > 0)
            {
                candidates.Add(stok.DepoMiktar.Value);
            }
            
            // MevcutMiktar: Helper property (DepoMiktar ?? StoMiktar)
            if (stok.MevcutMiktar.HasValue && stok.MevcutMiktar.Value > 0)
            {
                candidates.Add(stok.MevcutMiktar.Value);
            }
            
            // StoMiktar: Toplam stok (tüm depolar)
            if (stok.StoMiktar > 0)
            {
                candidates.Add(stok.StoMiktar);
            }
            
            // DepoStoklari toplamı
            if (stok.DepoStoklari?.Any() == true)
            {
                var depoToplam = stok.DepoStoklari.Sum(d => d.SatilabilirMiktar ?? d.StokMiktar);
                if (depoToplam > 0)
                {
                    candidates.Add(depoToplam);
                }
            }

            // En yüksek değeri döndür
            if (candidates.Count > 0)
            {
                var best = candidates.Max();
                return (int)Math.Max(0, Math.Floor(best));
            }

            // Hiçbir değer bulunamadı
            return 0;
        }

        /// <summary>
        /// Satılabilir (kullanılabilir) stok miktarını çözer.
        /// Rezerve miktarı düşülmüş net stok.
        /// </summary>
        private static int ResolveAvailableQuantityFromStok(MikroStokResponseDto stok, int? targetDepoNo = null)
        {
            // Önce KullanilabilirMiktar'ı kontrol et
            if (stok.KullanilabilirMiktar.HasValue && stok.KullanilabilirMiktar.Value > 0)
            {
                return (int)Math.Floor(stok.KullanilabilirMiktar.Value);
            }
            
            // Belirli depo için SatilabilirMiktar
            if (targetDepoNo.HasValue && targetDepoNo.Value > 0 && stok.DepoStoklari?.Any() == true)
            {
                var depoStok = stok.DepoStoklari.FirstOrDefault(d => d.DepNo == targetDepoNo.Value);
                if (depoStok?.SatilabilirMiktar.HasValue == true)
                {
                    return (int)Math.Max(0, Math.Floor(depoStok.SatilabilirMiktar.Value));
                }
            }

            // Fallback: Toplam stok - rezerve
            var total = ResolveStockQuantityFromStok(stok, targetDepoNo);
            var reserved = (int)Math.Max(0, Math.Floor(stok.RezervedMiktar ?? 0));
            return Math.Max(0, total - reserved);
        }

        public void UpdateProduct(MicroProductDto productDto)
        {
            // Fire-and-forget: tek ürün güncelleme — SaveStokV2Async üzerinden
            _ = Task.Run(async () =>
            {
                try
                {
                    var request = new MikroStokKaydetRequestDto
                    {
                        StoKod = productDto.Sku,
                        StoIsim = productDto.Name,
                        StoBirim1Ad = productDto.Unit,
                        StoToptanVergi = productDto.VatRate,
                        StoPerakendeVergi = productDto.VatRate,
                        StoAnagrupKod = productDto.CategoryCode,
                        SatisFiyatlari = productDto.Price > 0
                            ? new List<MikroStokFiyatDto> { new() { SfiyatFiyati = productDto.Price, SfiyatNo = 1 } }
                            : new List<MikroStokFiyatDto>()
                    };
                    var result = await SaveStokV2Async(request);
                    if (!result.Success)
                        _logger.LogWarning("[MicroService] UpdateProduct — {Sku} başarısız: {Msg}", productDto.Sku, result.Message);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[MicroService] UpdateProduct hata — {Sku}", productDto.Sku);
                }
            });
        }

        public Task<IEnumerable<MicroPriceDto>> GetPricesAsync()
        {
            return GetPricesFromSqlAsync();
        }

        public async Task<Dictionary<string, MikroFiyatSatirDto>> GetSqlPriceMapAsync(
            int? fiyatListesiNo = null,
            CancellationToken cancellationToken = default)
        {
            var normalizedListNo = fiyatListesiNo ?? -1;
            var now = DateTime.UtcNow;

            if (_sharedSqlPriceMap != null &&
                _sharedSqlPriceMapListNo == normalizedListNo &&
                (now - _sharedSqlPriceMapAtUtc) < SqlPriceMapCacheTtl)
            {
                _logger.LogDebug(
                    "[MicroService] Sql fiyat map cache kullanılıyor. FiyatListesiNo: {FiyatListesiNo}, Kayıt: {Count}",
                    normalizedListNo,
                    _sharedSqlPriceMap.Count);

                return new Dictionary<string, MikroFiyatSatirDto>(_sharedSqlPriceMap, StringComparer.OrdinalIgnoreCase);
            }

            await SqlPriceMapLock.WaitAsync(cancellationToken);
            try
            {
                now = DateTime.UtcNow;
                var staleMap = _sharedSqlPriceMap;
                if (_sharedSqlPriceMap != null &&
                    _sharedSqlPriceMapListNo == normalizedListNo &&
                    (now - _sharedSqlPriceMapAtUtc) < SqlPriceMapCacheTtl)
                {
                    return new Dictionary<string, MikroFiyatSatirDto>(_sharedSqlPriceMap, StringComparer.OrdinalIgnoreCase);
                }

                var priceRows = await GetSqlPriceRowsAsync(fiyatListesiNo, cancellationToken);

                var map = new Dictionary<string, MikroFiyatSatirDto>(StringComparer.OrdinalIgnoreCase);
                foreach (var row in priceRows)
                {
                    if (string.IsNullOrWhiteSpace(row.StokKod))
                    {
                        continue;
                    }

                    var normalizedStokKod = row.StokKod.Trim();
                    if (string.IsNullOrWhiteSpace(normalizedStokKod))
                    {
                        continue;
                    }

                    row.StokKod = normalizedStokKod;

                    if (!map.TryGetValue(normalizedStokKod, out var existing) ||
                        (existing.Fiyat <= 0 && row.Fiyat > 0))
                    {
                        map[normalizedStokKod] = row;
                    }
                }

                if (map.Count == 0 &&
                    staleMap != null &&
                    staleMap.Count > 0 &&
                    _sharedSqlPriceMapListNo == normalizedListNo)
                {
                    _logger.LogWarning(
                        "[MicroService] Sql fiyat map yenilemesi boş döndü. Son bilinen map kullanılacak. FiyatListesiNo: {FiyatListesiNo}, Kayıt: {Count}",
                        normalizedListNo,
                        staleMap.Count);

                    return new Dictionary<string, MikroFiyatSatirDto>(staleMap, StringComparer.OrdinalIgnoreCase);
                }

                _sharedSqlPriceMap = map;
                _sharedSqlPriceMapAtUtc = DateTime.UtcNow;
                _sharedSqlPriceMapListNo = normalizedListNo;

                return new Dictionary<string, MikroFiyatSatirDto>(map, StringComparer.OrdinalIgnoreCase);
            }
            finally
            {
                SqlPriceMapLock.Release();
            }
        }

        /// <summary>
        /// Admin mikro ekranı için ürün listesi döndürür.
        /// 
        /// NEDEN REFACTOR EDİLDİ:
        /// Eski akış: GetStockSnapshotAsync (StokListesiV2) → timeout → GetProductsFromSqlOnlyAsync
        /// Yeni akış: GetUnifiedProductMapAsync (tek SQL, lock + 60s cache)
        /// 
        /// CONCURRENCY FIX: Eski akışta N eşzamanlı istek, N ayrı SqlVeriOkuV2 isteği açıyordu.
        /// Yeni akışta UnifiedProductMapLock sayesinde yalnızca 1 SQL çalışır, kalanlar cache'ten döner.
        /// </summary>
        public async Task<List<MikroUrunDto>> GetProductsWithSqlAsync(
            int? depoNo = null,
            int? fiyatListesiNo = null,
            string? stokKod = null,
            string? grupKod = null,
            bool? sadeceStoklu = null,
            bool? sadeceAktif = true,
            int sayfaNo = 1,
            int sayfaBuyuklugu = 20,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var page = Math.Max(1, sayfaNo);
                var size = Math.Clamp(sayfaBuyuklugu, 1, 1000);
                var normalizedStokKod = string.IsNullOrWhiteSpace(stokKod) ? null : stokKod.Trim();
                var normalizedGrupKod = string.IsNullOrWhiteSpace(grupKod) ? null : grupKod.Trim();
                var resolvedDepoNo = depoNo ?? _settings.DefaultDepoNo;

                // Birleşik SQL sorgusu: lock + cache — eşzamanlı N istek → tek Mikro API çağrısı
                var unified = await GetUnifiedProductMapAsync(fiyatListesiNo, depoNo, cancellationToken);

                if (unified.Count == 0)
                {
                    _logger.LogWarning(
                        "[MicroService] GetProductsWithSqlAsync: Unified sorgu boş döndü (circuit breaker veya Mikro erişilemez).");
                    return new List<MikroUrunDto>();
                }

                IEnumerable<MikroUrunDto> projected = unified.Select(u => new MikroUrunDto
                {
                    StokKod     = u.StokKod,
                    UrunAdi     = u.StokAd,
                    Fiyat       = u.Fiyat,
                    StokMiktar  = u.StokMiktar,
                    DepoAdi     = $"Depo {(u.DepoNo ?? resolvedDepoNo)}",
                    DepoNo      = u.DepoNo ?? resolvedDepoNo,
                    IsWebActive = u.WebeGonderilecekFl,
                    Birim       = u.Birim,
                    GrupKod     = u.GrupKod,
                    KdvOrani    = u.KdvOrani,
                    Barkod      = u.Barkod ?? string.Empty
                });

                if (!string.IsNullOrWhiteSpace(normalizedStokKod))
                    projected = projected.Where(p => p.StokKod.Contains(normalizedStokKod, StringComparison.OrdinalIgnoreCase));

                if (!string.IsNullOrWhiteSpace(normalizedGrupKod))
                    projected = projected.Where(p => p.GrupKod.Contains(normalizedGrupKod, StringComparison.OrdinalIgnoreCase));

                if (sadeceStoklu.HasValue)
                    projected = sadeceStoklu.Value
                        ? projected.Where(p => p.StokMiktar > 0)
                        : projected.Where(p => p.StokMiktar <= 0);

                if (sadeceAktif == true)
                    projected = projected.Where(p => p.IsWebActive);

                var ordered    = projected.OrderBy(p => p.StokKod).ToList();
                var totalCount = ordered.Count;
                var pageItems  = ordered.Skip((page - 1) * size).Take(size).ToList();

                foreach (var item in pageItems)
                    item.ToplamKayit = totalCount;

                _logger.LogInformation(
                    "[MicroService] GetProductsWithSqlAsync tamamlandı. Toplam: {Total}, Sayfa: {Page}, Dönen: {Count}",
                    totalCount, page, pageItems.Count);

                return pageItems;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MicroService] GetProductsWithSqlAsync hatası.");
                return new List<MikroUrunDto>();
            }
        }

        private async Task<List<MikroUrunDto>> GetProductsFromSqlOnlyAsync(
            int? depoNo,
            int? fiyatListesiNo,
            string? normalizedStokKod,
            string? normalizedGrupKod,
            bool? sadeceStoklu,
            bool? sadeceAktif,
            int page,
            int size,
            CancellationToken cancellationToken)
        {
            var resolvedDepoNo = depoNo ?? _settings.DefaultDepoNo;

            var priceRows = await GetSqlPriceRowsAsync(fiyatListesiNo, cancellationToken);
            if (priceRows.Count == 0)
            {
                return new List<MikroUrunDto>();
            }

            var stockMap = await GetSqlStockMapAsync(depoNo, cancellationToken);

            var projected = priceRows
                .Where(r => !string.IsNullOrWhiteSpace(r.StokKod))
                .GroupBy(r => r.StokKod, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.OrderByDescending(x => x.Fiyat).First())
                .Select(row =>
                {
                    stockMap.TryGetValue(row.StokKod, out var sqlStockQty);

                    return new MikroUrunDto
                    {
                        StokKod = row.StokKod,
                        UrunAdi = string.IsNullOrWhiteSpace(row.UrunAdi) ? row.StokKod : row.UrunAdi,
                        Fiyat = row.Fiyat,
                        StokMiktar = sqlStockQty,
                        DepoAdi = $"Depo {resolvedDepoNo}",
                        DepoNo = resolvedDepoNo,
                        IsWebActive = row.WebeGonderilecekFl ?? true,
                        Birim = "ADET",
                        GrupKod = string.Empty
                    };
                });

            if (!string.IsNullOrWhiteSpace(normalizedStokKod))
            {
                projected = projected.Where(p => p.StokKod.Contains(normalizedStokKod, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(normalizedGrupKod))
            {
                projected = projected.Where(p => p.GrupKod.Contains(normalizedGrupKod, StringComparison.OrdinalIgnoreCase));
            }

            if (sadeceStoklu.HasValue)
            {
                projected = sadeceStoklu.Value
                    ? projected.Where(p => p.StokMiktar > 0)
                    : projected.Where(p => p.StokMiktar <= 0);
            }

            if (sadeceAktif == true)
            {
                projected = projected.Where(p => p.IsWebActive);
            }

            var ordered = projected
                .OrderBy(p => p.StokKod)
                .ToList();

            var totalCount = ordered.Count;
            var pageItems = ordered
                .Skip((page - 1) * size)
                .Take(size)
                .ToList();

            foreach (var item in pageItems)
            {
                item.ToplamKayit = totalCount;
            }

            return pageItems;
        }

        private async Task<IEnumerable<MicroPriceDto>> GetPricesFromSqlAsync(
            CancellationToken cancellationToken = default)
        {
            try
            {
                var rows = await GetSqlPriceRowsAsync(null, cancellationToken);
                return rows
                    .Where(x => !string.IsNullOrWhiteSpace(x.StokKod))
                    .GroupBy(x => x.StokKod, StringComparer.OrdinalIgnoreCase)
                    .Select(g => g.OrderByDescending(x => x.Fiyat).First())
                    .Select(x => new MicroPriceDto
                    {
                        Sku = x.StokKod,
                        Price = x.Fiyat,
                        Currency = "TRY",
                        EffectiveDate = DateTime.UtcNow
                    })
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MicroService] GetPricesAsync SqlVeriOkuV2 hatası.");
                return new List<MicroPriceDto>();
            }
        }

        private async Task<List<MikroFiyatSatirDto>> GetSqlPriceRowsAsync(
            int? fiyatListesiNo = null,
            CancellationToken cancellationToken = default)
        {
            // YENİ AKIŞ: Direkt DB — HTTP SqlVeriOkuV2 timeout'u ortadan kaldırıldı
            try
            {
                _logger.LogInformation(
                    "[MicroService] Fiyat satırları DB'den çekiliyor (direkt SQL). FiyatListesiNo: {FiyatListesiNo}",
                    fiyatListesiNo ?? -1);

                var rows = await _mikroDbService.GetFiyatSatirlariAsync(fiyatListesiNo, cancellationToken);

                _logger.LogInformation(
                    "[MicroService] Fiyat satırları DB sorgusu tamamlandı. Satır: {Count}",
                    rows.Count);

                return rows;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MicroService] GetSqlPriceRowsAsync DB hatası.");
                return [];
            }
        }

        /// <summary>
        /// Depo bazlı stok miktarlarını direkt DB'den çeker.
        /// NEDEN: Eski akış SqlVeriOkuV2 HTTP endpoint üzerindeydi → timeout.
        /// Yeni akış: IMikroDbService → direkt SqlConnection → 2s altı.
        /// </summary>
        public async Task<Dictionary<string, decimal>> GetSqlStockMapAsync(
            int? depoNo = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation(
                    "[MicroService] Stok miktarları DB'den çekiliyor (direkt SQL). DepoNo: {DepoNo}",
                    depoNo ?? -1);

                var map = await _mikroDbService.GetStokMiktarlariAsync(depoNo, cancellationToken);

                _logger.LogInformation(
                    "[MicroService] Stok miktarları DB sorgusu tamamlandı. Ürün: {Count}",
                    map.Count);

                return map;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MicroService] GetSqlStockMapAsync DB hatası.");
                return new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            }
        }

        /// <summary>
        /// Stok miktarını çekmek için SQL sorgusu oluşturur.
        /// Bu sorgu Mikro'daki stok kartlarından doğrudan stok miktarını çeker.
        /// </summary>
        private static string BuildSqlStockQuery(int? depoNo)
        {
            _ = depoNo; // View toplam stok döner — depo filtresi uygulanmıyor

            return @"SELECT
    S.sto_kod                                    AS stokkod,
    ISNULL(ST.stok_miktar, 0)                    AS stok_miktar
FROM STOKLAR S
LEFT JOIN (
    SELECT sth_stok_kod,
           SUM(ISNULL(sth_eldeki_miktar, 0)) AS stok_miktar
    FROM   STOK_HAREKETTEN_ELDEKI_MIKTAR_VIEW
    GROUP BY sth_stok_kod
) ST ON ST.sth_stok_kod = S.sto_kod
WHERE ISNULL(S.sto_webe_gonderilecek_fl, 0) = 1
  AND ISNULL(S.sto_iptal, 0) = 0
  AND S.sto_kod IS NOT NULL
  AND LTRIM(RTRIM(S.sto_kod)) <> ''
ORDER BY S.sto_kod;";
        }

        private bool ShouldSkipSqlReads(out TimeSpan remaining)
        {
            lock (CircuitStateLock)
            {
                var now = DateTime.UtcNow;
                if (_sqlTimeoutStrike >= SqlTimeoutStrikeThreshold && now < _skipSqlReadsUntilUtc)
                {
                    remaining = _skipSqlReadsUntilUtc - now;
                    return true;
                }

                // Cooldown süresi geçtiyse strike'ı otomatik temizle.
                if (now >= _skipSqlReadsUntilUtc && _sqlTimeoutStrike >= SqlTimeoutStrikeThreshold)
                {
                    _sqlTimeoutStrike = 0;
                    _skipSqlReadsUntilUtc = DateTime.MinValue;
                }

                remaining = TimeSpan.Zero;
                return false;
            }
        }

        private void RegisterSqlTimeout(string source)
        {
            lock (CircuitStateLock)
            {
                _sqlTimeoutStrike++;
                if (_sqlTimeoutStrike >= SqlTimeoutStrikeThreshold)
                {
                    _skipSqlReadsUntilUtc = DateTime.UtcNow.Add(SqlReadCooldown);
                    _logger.LogWarning(
                        "[MicroService] SqlVeriOkuV2 {Source} timeout kaydedildi. Strike: {Strike}, Cooldown aktif: {CooldownSeconds}s",
                        source,
                        _sqlTimeoutStrike,
                        (int)SqlReadCooldown.TotalSeconds);
                    return;
                }

                _logger.LogWarning(
                    "[MicroService] SqlVeriOkuV2 {Source} timeout kaydedildi. Strike: {Strike}/{Threshold}, Cooldown henüz aktif değil.",
                    source,
                    _sqlTimeoutStrike,
                    SqlTimeoutStrikeThreshold);
            }
        }

        private void RegisterSqlSuccess()
        {
            lock (CircuitStateLock)
            {
                if (_sqlTimeoutStrike == 0 && _skipSqlReadsUntilUtc <= DateTime.UtcNow)
                {
                    return;
                }

                _sqlTimeoutStrike = 0;
                _skipSqlReadsUntilUtc = DateTime.MinValue;
            }
        }

        /// <summary>
        /// SQL stok sorgusu sonucunu parse eder.
        /// </summary>
        private Dictionary<string, decimal> ParseSqlStockRows(string content)
        {
            var map = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

            try
            {
                using var doc = JsonDocument.Parse(content);
                var rowObjects = EnumerateSqlStockRowObjects(doc.RootElement).ToList();
                _logger.LogInformation("[MicroService] ParseSqlStockRows: toplam enumerasyon sayısı: {Count}", rowObjects.Count);
                
                foreach (var row in rowObjects)
                {
                    var stokKod = ReadStringFromRow(row, "stokkod", "sto_kod", "StoKod", "stokKod");
                    if (string.IsNullOrWhiteSpace(stokKod))
                    {
                        continue;
                    }

                    var stokRaw = ReadStringFromRow(row, "stok_miktar", "sto_miktar", "StokMiktar", "miktar");
                    var stokMiktar = ParseDecimalFlexible(stokRaw);

                    var normalizedStokKod = stokKod.Trim();
                    if (!map.ContainsKey(normalizedStokKod) || map[normalizedStokKod] < stokMiktar)
                    {
                        map[normalizedStokKod] = stokMiktar;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[MicroService] SqlVeriOkuV2 stok parse hatası.");
            }

            return map;
        }

        private static IEnumerable<JsonElement> EnumerateSqlStockRowObjects(JsonElement node)
        {
            if (node.ValueKind == JsonValueKind.Object)
            {
                if (LooksLikeSqlStockRow(node))
                {
                    yield return node;
                    yield break;
                }

                foreach (var prop in node.EnumerateObject())
                {
                    foreach (var child in EnumerateSqlStockRowObjects(prop.Value))
                    {
                        yield return child;
                    }
                }
            }
            else if (node.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in node.EnumerateArray())
                {
                    foreach (var child in EnumerateSqlStockRowObjects(item))
                    {
                        yield return child;
                    }
                }
            }
        }

        private static bool LooksLikeSqlStockRow(JsonElement obj)
        {
            if (obj.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            var hasStock = TryGetPropertyIgnoreCase(obj, "stokkod", out _) ||
                           TryGetPropertyIgnoreCase(obj, "sto_kod", out _) ||
                           TryGetPropertyIgnoreCase(obj, "stokkod", out _);

            var hasQuantity = TryGetPropertyIgnoreCase(obj, "stok_miktar", out _) ||
                              TryGetPropertyIgnoreCase(obj, "sto_miktar", out _) ||
                              TryGetPropertyIgnoreCase(obj, "miktar", out _);

            return hasStock && hasQuantity;
        }

        private static string BuildSqlPriceQuery(int? fiyatListesiNo)
        {
            // NEDEN: Web fiyat listesi Liste 11'e hazırlanır — default 2 hatalıydı
            var hedefListe = fiyatListesiNo is > 0 ? fiyatListesiNo.Value : 11;

            return $@"SELECT
    ISNULL(CONVERT(NVARCHAR(36), Hedef.sfiyat_Guid), '00000000-0000-0000-0000-000000000000') AS guid,
    S.sto_kod                                        AS stokkod,
    ISNULL(S.sto_isim, '')                           AS stokad,
    ISNULL(Hedef.sfiyat_fiyati, 0)                   AS fiyat,
    ISNULL(BK.bar_kodu, '-BARKODYOK-')               AS barkod,
    ISNULL(S.sto_webe_gonderilecek_fl, 0)            AS webe_gonderilecek_fl
FROM STOKLAR S
LEFT JOIN STOK_SATIS_FIYAT_LISTELERI Hedef
       ON  Hedef.sfiyat_stokkod     = S.sto_kod
       AND Hedef.sfiyat_listesirano = {hedefListe}
OUTER APPLY (
    SELECT TOP 1 bar_kodu
    FROM   BARKOD_TANIMLARI
    WHERE  bar_stokkodu = S.sto_kod
) BK
WHERE ISNULL(S.sto_webe_gonderilecek_fl, 0) = 1
  AND ISNULL(S.sto_iptal, 0) = 0
  AND S.sto_kod IS NOT NULL
  AND LTRIM(RTRIM(S.sto_kod)) <> ''
ORDER BY S.sto_kod;";
        }

        private List<MikroFiyatSatirDto> ParseSqlPriceRows(string content)
        {
            var rows = new List<MikroFiyatSatirDto>();

            try
            {
                using var doc = JsonDocument.Parse(content);
                var rowObjects = EnumerateSqlRowObjects(doc.RootElement).ToList();
                _logger.LogInformation("[MicroService] ParseSqlPriceRows: toplam enumerasyon sayısı: {Count}", rowObjects.Count);
                
                foreach (var row in rowObjects)
                {
                    var stokKod = ReadStringFromRow(row, "stokkod", "sfiyat_stokkod", "StoKod", "stokKod");
                    if (string.IsNullOrWhiteSpace(stokKod) || string.Equals(stokKod, "TANIMSIZ", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var fiyatRaw = ReadStringFromRow(row, "fiyat", "sfiyat_fiyati", "Price", "price");
                    var fiyat = ParseDecimalFlexible(fiyatRaw);

                    rows.Add(new MikroFiyatSatirDto
                    {
                        Guid = ReadStringFromRow(row, "guid", "sfiyat_guid", "Guid", "GUID"),
                        StokKod = stokKod.Trim(),
                        UrunAdi = ReadStringFromRow(row, "stokad", "sto_isim", "StokAd", "urunAdi"),
                        Fiyat = fiyat,
                        Barkod = ReadStringFromRow(row, "barkod", "bar_kodu", "Barkod", "barcode"),
                        WebeGonderilecekFl = ParseBoolFlexible(
                            ReadStringFromRow(
                                row,
                                "webe_gonderilecek_fl",
                                "sto_webe_gonderilecek_fl",
                                "WebeGonderilecekFl",
                                "webAktif"
                            )
                        )
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[MicroService] SqlVeriOkuV2 parse hatası.");
            }

            return rows;
        }

        private static IEnumerable<JsonElement> EnumerateSqlRowObjects(JsonElement node)
        {
            if (node.ValueKind == JsonValueKind.Object)
            {
                if (LooksLikeSqlRow(node))
                {
                    yield return node;
                    yield break;
                }

                foreach (var prop in node.EnumerateObject())
                {
                    foreach (var child in EnumerateSqlRowObjects(prop.Value))
                    {
                        yield return child;
                    }
                }
            }
            else if (node.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in node.EnumerateArray())
                {
                    foreach (var child in EnumerateSqlRowObjects(item))
                    {
                        yield return child;
                    }
                }
            }
        }

        private static bool LooksLikeSqlRow(JsonElement obj)
        {
            if (obj.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            var hasStock = TryGetPropertyIgnoreCase(obj, "stokkod", out _) ||
                           TryGetPropertyIgnoreCase(obj, "sfiyat_stokkod", out _);

            var hasPrice = TryGetPropertyIgnoreCase(obj, "fiyat", out _) ||
                           TryGetPropertyIgnoreCase(obj, "sfiyat_fiyati", out _);

            return hasStock && hasPrice;
        }

        private static bool TryGetPropertyIgnoreCase(JsonElement obj, string propertyName, out JsonElement value)
        {
            foreach (var prop in obj.EnumerateObject())
            {
                if (string.Equals(prop.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    value = prop.Value;
                    return true;
                }
            }

            value = default;
            return false;
        }

        private static string ReadStringFromRow(JsonElement row, params string[] keys)
        {
            foreach (var key in keys)
            {
                if (!TryGetPropertyIgnoreCase(row, key, out var value))
                {
                    continue;
                }

                return value.ValueKind switch
                {
                    JsonValueKind.String => value.GetString() ?? string.Empty,
                    JsonValueKind.Number => value.ToString(),
                    JsonValueKind.True => "true",
                    JsonValueKind.False => "false",
                    _ => string.Empty
                };
            }

            return string.Empty;
        }

        private static decimal ParseDecimalFlexible(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return 0m;
            }

            var cleaned = Regex.Replace(input, "[^0-9,.-]", string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(cleaned))
            {
                return 0m;
            }

            var hasDot = cleaned.Contains('.');
            var hasComma = cleaned.Contains(',');

            if (hasDot && hasComma)
            {
                var lastDot = cleaned.LastIndexOf('.');
                var lastComma = cleaned.LastIndexOf(',');

                if (lastComma > lastDot)
                {
                    cleaned = cleaned.Replace(".", string.Empty).Replace(',', '.');
                }
                else
                {
                    cleaned = cleaned.Replace(",", string.Empty);
                }
            }
            else if (hasComma)
            {
                cleaned = cleaned.Replace(',', '.');
            }

            return decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : 0m;
        }

        private static bool? ParseBoolFlexible(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return null;
            }

            var normalized = input.Trim().ToLowerInvariant();
            if (normalized is "1" or "true" or "evet" or "yes")
            {
                return true;
            }

            if (normalized is "0" or "false" or "hayir" or "hayır" or "no")
            {
                return false;
            }

            return null;
        }

        private static decimal ResolveProductPriceFromSources(
            MikroStokResponseDto stok,
            IReadOnlyDictionary<string, MikroFiyatSatirDto> priceMap)
        {
            var sku = stok.StoKod ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(sku) && priceMap.TryGetValue(sku, out var priceRow) && priceRow.Fiyat > 0)
            {
                return priceRow.Fiyat;
            }

            return ResolveProductPriceFromStok(stok);
        }

        private static string? ResolveBarcodeFromSources(
            MikroStokResponseDto stok,
            IReadOnlyDictionary<string, MikroFiyatSatirDto> priceMap)
        {
            if (!string.IsNullOrWhiteSpace(stok.Barkod))
            {
                return stok.Barkod;
            }

            var sku = stok.StoKod ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(sku) &&
                priceMap.TryGetValue(sku, out var priceRow) &&
                !string.IsNullOrWhiteSpace(priceRow.Barkod) &&
                !string.Equals(priceRow.Barkod, "-BARKODYOK-", StringComparison.OrdinalIgnoreCase))
            {
                return priceRow.Barkod;
            }

            return stok.Barkod;
        }

        public Task<IEnumerable<MicroCustomerDto>> GetCustomersAsync()
        {
            // TODO: CariListesiV2 endpoint'i ile değiştirilecek (Adım 3'te)
            _logger.LogWarning("[MicroService] GetCustomersAsync henüz MikroAPI V2'ye migrate edilmedi.");
            return Task.FromResult<IEnumerable<MicroCustomerDto>>(new List<MicroCustomerDto>());
        }

        public async Task<bool> UpsertProductsAsync(IEnumerable<MicroProductDto> products)
        {
            // SaveStokV2Async üzerinden Mikro ERP'ye ürün yazma
            var productList = products.ToList();
            if (productList.Count == 0) return true;

            _logger.LogInformation("[MicroService] UpsertProductsAsync başlatılıyor. Ürün sayısı: {Count}", productList.Count);

            int success = 0, fail = 0;
            foreach (var product in productList)
            {
                try
                {
                    var request = new MikroStokKaydetRequestDto
                    {
                        StoKod = product.Sku,
                        StoIsim = product.Name,
                        StoBirim1Ad = product.Unit,
                        StoToptanVergi = product.VatRate,
                        StoPerakendeVergi = product.VatRate,
                        StoAnagrupKod = product.CategoryCode,
                        Barkodlar = !string.IsNullOrEmpty(product.Barcode)
                            ? new List<MikroStokBarkodDto> { new() { BarBarkodNo = product.Barcode, BarCarpan = 1 } }
                            : new List<MikroStokBarkodDto>(),
                        SatisFiyatlari = product.Price > 0
                            ? new List<MikroStokFiyatDto> { new() { SfiyatFiyati = product.Price, SfiyatNo = 1 } }
                            : new List<MikroStokFiyatDto>()
                    };

                    var result = await SaveStokV2Async(request);
                    if (result.Success) success++;
                    else
                    {
                        fail++;
                        _logger.LogWarning("[MicroService] UpsertProducts — {Sku} başarısız: {Msg}", product.Sku, result.Message);
                    }
                }
                catch (Exception ex)
                {
                    fail++;
                    _logger.LogError(ex, "[MicroService] UpsertProducts — {Sku} hata", product.Sku);
                }
            }

            _logger.LogInformation("[MicroService] UpsertProductsAsync tamamlandı. Başarılı: {Ok}, Başarısız: {Fail}", success, fail);
            return fail == 0;
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
            if (ShouldSkipStokReads(out var remaining))
            {
                _logger.LogWarning(
                    "[MicroService] StokListesiV2 çağrısı cooldown nedeniyle atlandı. Kalan bekleme: {RemainingSeconds}s",
                    Math.Max(0, (int)remaining.TotalSeconds));

                return new MikroResponseWrapper<MikroStokResponseDto>
                {
                    Success = false,
                    Message = "Stok servisi geçici olarak yavaş yanıt veriyor. Kısa süre sonra tekrar deneyin."
                };
            }

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
                using var stokTimeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                stokTimeoutCts.CancelAfter(StokListesiTimeout);

                var response = await SendMikroRequestAsync(StokListesiV2Endpoint, requestBody, stokTimeoutCts.Token, 1);
                
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

                RegisterStokSuccess();

                // Response'u parse et
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                
                _logger.LogInformation(
                    "[MicroService] StokListesiV2 RAW Response (ilk 500 karakter): {Content}",
                    content.Length > 500 ? content.Substring(0, 500) + "..." : content);
                
                var result = JsonSerializer.Deserialize<MikroResponseWrapper<MikroStokResponseDto>>(
                    content, _jsonOptions);

                // Mikro API bazen standart success/data yerine result[0].Data.StokListesi formatı döndürüyor.
                // Bu durumda fallback parser ile veriyi normalize et.
                if (result == null || (!result.Success && (result.Data == null || result.Data.Count == 0)))
                {
                    var legacyParsed = TryParseLegacyStokListesiResponse(content);
                    if (legacyParsed != null)
                    {
                        result = legacyParsed;
                    }
                }

                _logger.LogInformation(
                    "[MicroService] StokListesiV2 tamamlandı. Success: {Success}, Kayıt: {Count}, Toplam: {Total}, Message: {Message}",
                    result?.Success ?? false, result?.Data?.Count ?? 0, result?.TotalCount ?? 0, result?.Message ?? "null");

                return result ?? new MikroResponseWrapper<MikroStokResponseDto> { Success = false, Message = "Parse hatası" };
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                RegisterStokTimeout();
                _logger.LogWarning(
                    "[MicroService] StokListesiV2 timeout. Timeout: {TimeoutSeconds}s",
                    (int)StokListesiTimeout.TotalSeconds);

                return new MikroResponseWrapper<MikroStokResponseDto>
                {
                    Success = false,
                    Message = "Stok servisi zaman aşımına uğradı."
                };
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

        private bool ShouldSkipStokReads(out TimeSpan remaining)
        {
            lock (CircuitStateLock)
            {
                var now = DateTime.UtcNow;
                if (_stokTimeoutStrike >= StokTimeoutStrikeThreshold && now < _skipStokReadsUntilUtc)
                {
                    remaining = _skipStokReadsUntilUtc - now;
                    return true;
                }

                if (now >= _skipStokReadsUntilUtc && _stokTimeoutStrike >= StokTimeoutStrikeThreshold)
                {
                    _stokTimeoutStrike = 0;
                    _skipStokReadsUntilUtc = DateTime.MinValue;
                }

                remaining = TimeSpan.Zero;
                return false;
            }
        }

        private void RegisterStokTimeout()
        {
            lock (CircuitStateLock)
            {
                _stokTimeoutStrike++;
                if (_stokTimeoutStrike >= StokTimeoutStrikeThreshold)
                {
                    _skipStokReadsUntilUtc = DateTime.UtcNow.Add(StokReadCooldown);
                    _logger.LogWarning(
                        "[MicroService] StokListesiV2 timeout kaydedildi. Strike: {Strike}, Cooldown aktif: {CooldownSeconds}s",
                        _stokTimeoutStrike,
                        (int)StokReadCooldown.TotalSeconds);
                    return;
                }

                _logger.LogWarning(
                    "[MicroService] StokListesiV2 timeout kaydedildi. Strike: {Strike}/{Threshold}, Cooldown henüz aktif değil.",
                    _stokTimeoutStrike,
                    StokTimeoutStrikeThreshold);
            }
        }

        private void RegisterStokSuccess()
        {
            lock (CircuitStateLock)
            {
                if (_stokTimeoutStrike == 0 && _skipStokReadsUntilUtc <= DateTime.UtcNow)
                {
                    return;
                }

                _stokTimeoutStrike = 0;
                _skipStokReadsUntilUtc = DateTime.MinValue;
            }
        }

        /// <summary>
        /// Mikro'nun legacy response formatını parse eder:
        /// { "result": [ { "StatusCode": 200, "Data": { "StokListesi": [...] } } ] }
        /// </summary>
        private MikroResponseWrapper<MikroStokResponseDto>? TryParseLegacyStokListesiResponse(string content)
        {
            try
            {
                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;

                if (!root.TryGetProperty("result", out var resultArray) ||
                    resultArray.ValueKind != JsonValueKind.Array ||
                    resultArray.GetArrayLength() == 0)
                {
                    return null;
                }

                var first = resultArray[0];

                var successFromPayload = first.TryGetProperty("success", out var successEl) &&
                                         (successEl.ValueKind == JsonValueKind.True || successEl.ValueKind == JsonValueKind.False)
                    ? successEl.GetBoolean()
                    : true;

                var statusCode = first.TryGetProperty("StatusCode", out var statusEl) && statusEl.ValueKind == JsonValueKind.Number
                    ? statusEl.GetInt32()
                    : 0;

                var isError = first.TryGetProperty("IsError", out var isErrorEl) &&
                              (isErrorEl.ValueKind == JsonValueKind.True || isErrorEl.ValueKind == JsonValueKind.False)
                    ? isErrorEl.GetBoolean()
                    : false;

                var errorMessage = first.TryGetProperty("ErrorMessage", out var errEl) && errEl.ValueKind == JsonValueKind.String
                    ? errEl.GetString() ?? string.Empty
                    : string.Empty;

                if (string.IsNullOrWhiteSpace(errorMessage) &&
                    first.TryGetProperty("errorText", out var errorTextEl) &&
                    errorTextEl.ValueKind == JsonValueKind.String)
                {
                    errorMessage = errorTextEl.GetString() ?? string.Empty;
                }

                if (statusCode != 200 || isError || !successFromPayload)
                {
                    return new MikroResponseWrapper<MikroStokResponseDto>
                    {
                        Success = false,
                        Message = string.IsNullOrWhiteSpace(errorMessage)
                            ? $"Mikro API StatusCode: {statusCode}"
                            : errorMessage,
                        Data = new List<MikroStokResponseDto>()
                    };
                }

                if (!first.TryGetProperty("Data", out var dataObj) || dataObj.ValueKind != JsonValueKind.Object)
                {
                    return new MikroResponseWrapper<MikroStokResponseDto>
                    {
                        Success = true,
                        Message = "Veri boş döndü",
                        Data = new List<MikroStokResponseDto>(),
                        TotalCount = 0
                    };
                }

                if (!dataObj.TryGetProperty("StokListesi", out var stokListEl) || stokListEl.ValueKind != JsonValueKind.Array)
                {
                    return new MikroResponseWrapper<MikroStokResponseDto>
                    {
                        Success = true,
                        Message = "StokListesi bulunamadı",
                        Data = new List<MikroStokResponseDto>(),
                        TotalCount = 0
                    };
                }

                var stoklar = JsonSerializer.Deserialize<List<MikroStokResponseDto>>(stokListEl.GetRawText(), _jsonOptions)
                             ?? new List<MikroStokResponseDto>();

                int? totalCount = null;
                if (dataObj.TryGetProperty("ToplamKayit", out var toplamKayitEl) && toplamKayitEl.ValueKind == JsonValueKind.Number)
                {
                    totalCount = toplamKayitEl.GetInt32();
                }
                else if (dataObj.TryGetProperty("TotalCount", out var totalCountEl) && totalCountEl.ValueKind == JsonValueKind.Number)
                {
                    totalCount = totalCountEl.GetInt32();
                }

                return new MikroResponseWrapper<MikroStokResponseDto>
                {
                    Success = true,
                    Message = string.Empty,
                    Data = stoklar,
                    // TotalCount bazı legacy cevaplarda gelmiyor; null bırakılırsa
                    // çağıran taraf veri bitene kadar sayfalamaya devam eder.
                    TotalCount = totalCount
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[MicroService] Legacy StokListesi response parse edilemedi.");
                return null;
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

        // ==================== TOPLU İŞLEMLER (V2) — LEGACY ====================

        /// <summary>
        /// [LEGACY] Tüm stokları paralel sayfa çekme ile hızlı getirir.
        /// NEDEN DEPRECATED: Birleşik SQL sorgusu (GetUnifiedProductMapAsync) tek istekle tüm veriyi çeker.
        /// Sayfalı paralel çekim artık gereksiz — 180s+ timeout sorunları biter.
        /// </summary>
        [Obsolete("Birleşik SQL akışı (GetUnifiedProductMapAsync) kullanın. Sayfalı StokListesiV2 çekimi gereksizleşti.")]
        public async Task<List<MikroStokResponseDto>> GetAllStokParallelAsync(
            int pageSize = 500,
            int? depoNo = null,
            CancellationToken cancellationToken = default)
        {
            var allStocks = new List<MikroStokResponseDto>();
            var parallelCount = _settings.ParallelPageFetchCount > 0 ? _settings.ParallelPageFetchCount : 5;
            const int hardMaxPages = 100; // 100 sayfa x 500 = 50.000 ürün kapasitesi
            
            _logger.LogInformation(
                "[MicroService] Paralel stok senkronizasyonu başlıyor. Depo: {Depo}, ParalelSayfa: {Parallel}, SayfaBoyutu: {PageSize}",
                depoNo ?? _settings.DefaultDepoNo, parallelCount, pageSize);

            // Önce ilk sayfayı çekerek toplam kayıt sayısını öğren
            var firstPageRequest = new MikroStokListesiRequestDto
            {
                SayfaNo = 1,
                SayfaBuyuklugu = pageSize,
                DepoNo = depoNo ?? _settings.DefaultDepoNo,
                FiyatDahil = true,
                BarkodDahil = true
            };

            var firstResult = await GetStokListesiV2Async(firstPageRequest, cancellationToken);
            
            if (!firstResult.Success || firstResult.Data == null || firstResult.Data.Count == 0)
            {
                _logger.LogWarning("[MicroService] İlk sayfa boş veya başarısız.");
                return allStocks;
            }

            allStocks.AddRange(firstResult.Data);
            
            // TotalCount yoksa 50 sayfaya sabitlemek yerine güvenli şekilde sayfa sayfa ilerle.
            // Mikro bazı ortamlarda toplam kayıt bilgisini dönmüyor (Toplam: 0).
            if (!firstResult.TotalCount.HasValue || firstResult.TotalCount.Value <= 0)
            {
                const int maxPagesWithoutTotalCount = hardMaxPages;
                string? lastPageFirstSku = firstResult.Data.FirstOrDefault()?.StoKod;
                var seenFirstSkus = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                if (!string.IsNullOrWhiteSpace(lastPageFirstSku))
                {
                    seenFirstSkus.Add(lastPageFirstSku);
                }

                _logger.LogWarning(
                    "[MicroService] TotalCount dönmedi (Toplam: 0). Güvenli sayfalama moduna geçiliyor.");

                for (int pageNo = 2; pageNo <= maxPagesWithoutTotalCount; pageNo++)
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
                        _logger.LogInformation(
                            "[MicroService] TotalCount yok modunda veri bitti. Sayfa: {Page}",
                            pageNo);
                        break;
                    }

                    allStocks.AddRange(result.Data);

                    var currentPageFirstSku = result.Data.FirstOrDefault()?.StoKod;
                    if (!string.IsNullOrWhiteSpace(currentPageFirstSku))
                    {
                        if (currentPageFirstSku == lastPageFirstSku || !seenFirstSkus.Add(currentPageFirstSku))
                        {
                            _logger.LogWarning(
                                "[MicroService] TotalCount yok modunda sayfa tekrarına rastlandı. Sayfa: {Page}, İlk SKU: {Sku}. Döngü sonlandırıldı.",
                                pageNo,
                                currentPageFirstSku);
                            break;
                        }

                        lastPageFirstSku = currentPageFirstSku;
                    }

                    if (result.Data.Count < pageSize)
                    {
                        _logger.LogInformation(
                            "[MicroService] Son sayfa tespit edildi. Sayfa: {Page}, Kayıt: {Count}",
                            pageNo,
                            result.Data.Count);
                        break;
                    }
                }

                _logger.LogInformation(
                    "[MicroService] TotalCount yok modunda stok senkronizasyonu tamamlandı. Toplam: {Count}",
                    allStocks.Count);

                return allStocks;
            }

            // Toplam sayfa sayısını hesapla (TotalCount varsa)
            int totalRecords = firstResult.TotalCount.Value;
            int totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);
            totalPages = Math.Min(totalPages, hardMaxPages);

            _logger.LogInformation(
                "[MicroService] İlk sayfa alındı. Toplam kayıt: {Total}, Toplam sayfa: {Pages}",
                totalRecords, totalPages);

            if (totalPages <= 1)
            {
                return allStocks;
            }

            // Kalan sayfaları paralel çek
            var pagesToFetch = Enumerable.Range(2, totalPages - 1).ToList();
            var allPageResults = new System.Collections.Concurrent.ConcurrentBag<(int Page, List<MikroStokResponseDto> Data)>();
            
            // Paralel gruplar halinde çek
            var semaphore = new SemaphoreSlim(parallelCount);
            var tasks = pagesToFetch.Select(async pageNo =>
            {
                await semaphore.WaitAsync(cancellationToken);
                try
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
                    
                    if (result.Success && result.Data != null && result.Data.Count > 0)
                    {
                        allPageResults.Add((pageNo, result.Data));
                        _logger.LogDebug(
                            "[MicroService] Paralel sayfa {Page}/{Total} tamamlandı. Kayıt: {Count}",
                            pageNo, totalPages, result.Data.Count);
                    }
                    else if (result.Data == null || result.Data.Count == 0)
                    {
                        // Boş sayfa - daha fazla veri yok
                        _logger.LogDebug("[MicroService] Sayfa {Page} boş, muhtemelen son sayfa geçildi.", pageNo);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[MicroService] Sayfa {Page} çekilemedi.", pageNo);
                }
                finally
                {
                    semaphore.Release();
                }
            });

            await Task.WhenAll(tasks);

            // Sonuçları sayfa sırasına göre sırala ve ekle
            var orderedResults = allPageResults.OrderBy(x => x.Page).SelectMany(x => x.Data);
            allStocks.AddRange(orderedResults);

            _logger.LogInformation(
                "[MicroService] Paralel stok senkronizasyonu tamamlandı. Toplam: {Count} kayıt",
                allStocks.Count);

            return allStocks;
        }

        /// <summary>
        /// [LEGACY] Tüm stokları sayfalı olarak çeker (full sync).
        /// NEDEN DEPRECATED: Birleşik SQL sorgusu ile sayfalı çekim gereksizleşti.
        /// Tek SQL sorgusu sadece web-aktif ürünleri çeker (~500-1200 vs 6000+).
        /// </summary>
        [Obsolete("Birleşik SQL akışı (GetUnifiedProductMapAsync) kullanın. Sayfalı StokListesiV2 çekimi gereksizleşti.")]
        public async IAsyncEnumerable<MikroStokResponseDto> GetAllStokAsync(
            int pageSize = 500,
            int? depoNo = null,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            int pageNo = 1;
            bool hasMore = true;
            int yieldedCount = 0;
            string? lastPageFirstSku = null;
            var seenFirstSkus = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            const int maxPagesWithoutTotalCount = 100; // TotalCount yoksa max 100 sayfa
            const int hardMaxPages = 100; // Kesin üst sınır: 100 sayfa x 500 = 50.000 ürün

            _logger.LogInformation(
                "[MicroService] Tam stok senkronizasyonu başlıyor. Depo: {Depo}, SayfaBoyutu: {PageSize}",
                depoNo ?? -1, pageSize);

            while (hasMore && !cancellationToken.IsCancellationRequested)
            {
                if (pageNo > hardMaxPages)
                {
                    _logger.LogWarning(
                        "[MicroService] Sayfa üst sınırına ulaşıldı ({MaxPages}). Döngü sonlandırıldı.",
                        hardMaxPages);
                    yield break;
                }

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
                    yieldedCount++;
                    yield return stok;
                }

                // Mikro API bazı ortamlarda istenen pageSize'tan daha az kayıt döndürebilir (örn. max 20).
                // Bu yüzden devam kararını mümkünse TotalCount ile, yoksa veri bitene kadar sürdür.
                if (result.TotalCount.HasValue && result.TotalCount.Value > 0)
                {
                    hasMore = yieldedCount < result.TotalCount.Value;
                }
                else
                {
                    // TotalCount gelmiyorsa en güvenli strateji:
                    // 1) Sayfa boyutu dolu değilse bitti kabul et
                    // 2) İlk SKU tekrar ederse API sayfaları döndürüp tekrar ediyor olabilir, döngüyü kes
                    // 3) Üst güvenlik limiti ile sonsuz döngüyü engelle
                    hasMore = result.Data.Count >= pageSize;

                    if (pageNo >= maxPagesWithoutTotalCount)
                    {
                        _logger.LogWarning(
                            "[MicroService] TotalCount olmadan maksimum sayfa limitine ulaşıldı ({MaxPages}). Döngü sonlandırıldı.",
                            maxPagesWithoutTotalCount);
                        hasMore = false;
                    }
                }

                // Koruma: Aynı ilk SKU tekrar geliyorsa sayfalama ilerlemiyor olabilir.
                var currentPageFirstSku = result.Data.FirstOrDefault()?.StoKod;
                if (hasMore && !string.IsNullOrWhiteSpace(currentPageFirstSku) && currentPageFirstSku == lastPageFirstSku)
                {
                    _logger.LogWarning(
                        "[MicroService] Sayfalama ilerlemiyor olabilir. Sayfa: {Page}, İlk SKU: {Sku}. Döngü sonlandırıldı.",
                        pageNo, currentPageFirstSku);
                    hasMore = false;
                }

                if (hasMore && !result.TotalCount.HasValue && !string.IsNullOrWhiteSpace(currentPageFirstSku))
                {
                    if (!seenFirstSkus.Add(currentPageFirstSku))
                    {
                        _logger.LogWarning(
                            "[MicroService] TotalCount yok ve ilk SKU tekrar etti. Sayfa: {Page}, İlk SKU: {Sku}. Döngü sonlandırıldı.",
                            pageNo, currentPageFirstSku);
                        hasMore = false;
                    }
                }

                lastPageFirstSku = currentPageFirstSku;
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

        // ==================== SİPARİŞ TESLİM MİKTARLARI ====================

        /// <summary>
        /// Mikro'dan sipariş teslim miktarlarını (tartı sonuçları) çeker.
        /// NEDEN: Mağaza personeli ürünleri tartıp Mikro'ya girer,
        /// biz sip_teslim_miktar alanından gerçek miktarları çekeriz.
        ///
        /// AKIŞ:
        /// 1. SiparisListesiV2 endpoint'inden siparişleri çek
        /// 2. sip_ozel_kod == orderNumber olan siparişi bul (e-ticaret referansı)
        /// 3. Her satırdan sip_miktar, sip_teslim_miktar, sip_b_fiyat değerlerini al
        /// 4. MikroDeliveryWeightsResult olarak döndür
        /// </summary>
        /// <param name="orderNumber">E-ticaret sipariş numarası (Mikro'daki sip_ozel_kod)</param>
        /// <param name="cancellationToken">İptal token'ı</param>
        /// <returns>Teslim miktarları sonucu veya hata durumunda null</returns>
        public async Task<MikroDeliveryWeightsResult?> GetOrderDeliveryWeightsAsync(
            string orderNumber, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(orderNumber))
            {
                _logger.LogWarning("[MicroService] GetOrderDeliveryWeightsAsync: orderNumber boş.");
                return new MikroDeliveryWeightsResult
                {
                    Success = false,
                    ErrorMessage = "Sipariş numarası boş olamaz.",
                    OrderNumber = orderNumber ?? string.Empty
                };
            }

            try
            {
                _logger.LogInformation(
                    "[MicroService] GetOrderDeliveryWeightsAsync çağrılıyor. Sipariş: {OrderNumber}",
                    orderNumber);

                // SiparisListesiV2 endpoint'ini kullanarak siparişleri çek.
                // EvrakSeri filtresi ile sadece online siparişleri (varsayılan seri) alalım.
                var request = new MikroSiparisListesiRequestDto
                {
                    EvrakSeri = _settings.DefaultEvrakSeri,
                    SayfaBuyuklugu = 100 // Yeterli büyüklükte sayfa
                };

                var siparisResult = await GetSiparisListesiV2Async(request, cancellationToken);

                if (!siparisResult.Success || siparisResult.Data == null)
                {
                    _logger.LogWarning(
                        "[MicroService] GetOrderDeliveryWeightsAsync: SiparisListesiV2 başarısız. " +
                        "Sipariş: {OrderNumber}, Mesaj: {Message}",
                        orderNumber, siparisResult.Message);

                    return new MikroDeliveryWeightsResult
                    {
                        Success = false,
                        ErrorMessage = $"Mikro sipariş listesi alınamadı: {siparisResult.Message}",
                        OrderNumber = orderNumber
                    };
                }

                // sip_ozel_kod == orderNumber olan siparişi bul (e-ticaret referansı)
                var matchedOrder = siparisResult.Data
                    .FirstOrDefault(s => string.Equals(
                        s.SipOzelKod?.Trim(), orderNumber.Trim(), StringComparison.OrdinalIgnoreCase));

                if (matchedOrder == null)
                {
                    _logger.LogWarning(
                        "[MicroService] GetOrderDeliveryWeightsAsync: Sipariş bulunamadı. " +
                        "OrderNumber: {OrderNumber}, Toplam sipariş: {Count}",
                        orderNumber, siparisResult.Data.Count);

                    return new MikroDeliveryWeightsResult
                    {
                        Success = false,
                        ErrorMessage = $"Mikro'da sip_ozel_kod='{orderNumber}' ile eşleşen sipariş bulunamadı.",
                        OrderNumber = orderNumber
                    };
                }

                // Sipariş satırlarından teslim miktarlarını çıkar
                var items = new List<MikroDeliveryWeightItem>();

                if (matchedOrder.Satirlar != null)
                {
                    foreach (var satir in matchedOrder.Satirlar)
                    {
                        items.Add(new MikroDeliveryWeightItem
                        {
                            StokKod = satir.SipStokKod,
                            StokIsim = satir.SipStokIsim,
                            SiparisMiktar = satir.SipMiktar,
                            TeslimMiktar = satir.SipTeslimMiktar ?? 0m,
                            BirimFiyat = satir.SipBFiyat
                        });
                    }
                }

                _logger.LogInformation(
                    "[MicroService] GetOrderDeliveryWeightsAsync tamamlandı. " +
                    "Sipariş: {OrderNumber}, Satır: {Count}, " +
                    "Teslim edilen: {DeliveredCount}",
                    orderNumber, items.Count,
                    items.Count(i => i.TeslimMiktar > 0));

                return new MikroDeliveryWeightsResult
                {
                    Success = true,
                    OrderNumber = orderNumber,
                    Items = items
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[MicroService] GetOrderDeliveryWeightsAsync hatası. Sipariş: {OrderNumber}",
                    orderNumber);

                return new MikroDeliveryWeightsResult
                {
                    Success = false,
                    ErrorMessage = $"Mikro teslim miktarları alınırken hata: {ex.Message}",
                    OrderNumber = orderNumber
                };
            }
        }

        // ==================== BİRLEŞİK SQL SORGUSU (UNIFIED) ====================

        // Birleşik sorgu cache alanları — StokListesiV2 yerine tek SQL ile tüm web ürünlerini çekeriz.
        // NEDEN: 2 ayrı SqlVeriOkuV2 + sayfalı StokListesiV2 çağrıları yerine tek istek → %80+ yük azalması.
        private static readonly SemaphoreSlim UnifiedProductMapLock = new(1, 1);
        private static List<MikroUnifiedProductDto>? _sharedUnifiedProductMap;
        private static DateTime _sharedUnifiedProductMapAtUtc;
        private static int _sharedUnifiedProductMapFiyatListesiNo = int.MinValue; // Cache key
        private static readonly TimeSpan UnifiedProductMapCacheTtl = TimeSpan.FromSeconds(60);

        /// <summary>
        /// Fiyat + stok + ürün bilgisi + barkod verilerini TEK SQL sorgusuyla çeken birleşik sorgu oluşturur.
        /// 
        /// FİYAT: PrepareWebPriceListAsync ile liste 11 önceden doldurulduğu için
        /// doğrudan hedef listeden okunur. Fallback: Liste 1 Depo 1 (ana perakende deposu).
        /// </summary>
        private static string BuildUnifiedProductQuery(int? fiyatListesiNo, int? depoNo)
        {
            // NEDEN: Web fiyat listesi Liste 11'e hazırlanır — default 2 hatalıydı
            var hedefListe = fiyatListesiNo is > 0 ? fiyatListesiNo.Value : 11;
            var hedefDepo  = depoNo.HasValue ? depoNo.Value : 0;

            return $@"SELECT
    S.sto_kod                                     AS stokkod,
    ISNULL(S.sto_isim, '')                        AS stokad,
    COALESCE(
        NULLIF(Hedef.sfiyat_fiyati, 0),
        NULLIF(Kaynak.MaxFiyat, 0),
        0
    )                                             AS fiyat,
    ISNULL(ST.stok_miktar, 0)                     AS stok_miktar,
    {hedefDepo}                                   AS depo_no,
    ISNULL(BK.bar_kodu, '')                       AS barkod,
    ISNULL(S.sto_altgrup_kod, '')                 AS grup_kod,
    ISNULL(S.sto_anagrup_kod, '')                 AS anagrup_kod,
    ISNULL(S.sto_birim1_ad, 'ADET')               AS birim,
    CASE ISNULL(S.sto_perakende_vergi, 0)
        WHEN 0 THEN 0
        WHEN 1 THEN 0
        WHEN 2 THEN 1
        WHEN 3 THEN 10
        WHEN 4 THEN 10
        WHEN 5 THEN 10
        WHEN 6 THEN 20
        ELSE 20
    END                                           AS kdv_orani,
    1                                             AS webe_gonderilecek_fl,
    NULL                                          AS son_hareket_tarihi
FROM STOKLAR S
LEFT JOIN STOK_SATIS_FIYAT_LISTELERI Hedef
       ON  Hedef.sfiyat_stokkod     = S.sto_kod
       AND Hedef.sfiyat_listesirano = {hedefListe}
       AND Hedef.sfiyat_deposirano  = {hedefDepo}
-- Fallback: Orijinal fiyat listesi (1), Depo 1 — PrepareWebPriceListAsync çalışmadıysa buradan oku
LEFT JOIN (
    SELECT sfiyat_stokkod, MAX(sfiyat_fiyati) AS MaxFiyat
    FROM   STOK_SATIS_FIYAT_LISTELERI
    WHERE  sfiyat_listesirano = 1
      AND  sfiyat_deposirano  = 1
    GROUP BY sfiyat_stokkod
) Kaynak ON Kaynak.sfiyat_stokkod = S.sto_kod
LEFT JOIN (
    SELECT sth_stok_kod,
           SUM(ISNULL(sth_eldeki_miktar, 0)) AS stok_miktar
    FROM   STOK_HAREKETTEN_ELDEKI_MIKTAR_VIEW
    GROUP BY sth_stok_kod
) ST ON ST.sth_stok_kod = S.sto_kod
OUTER APPLY (
    SELECT TOP 1 bar_kodu
    FROM   BARKOD_TANIMLARI
    WHERE  bar_stokkodu = S.sto_kod
) BK
WHERE S.sto_webe_gonderilecek_fl = 1
  AND ISNULL(S.sto_iptal, 0) = 0
  AND S.sto_kod IS NOT NULL
  AND LTRIM(RTRIM(S.sto_kod)) <> ''
ORDER BY S.sto_kod;";
        }

        /// <summary>
        /// Mikro ERP'den TEK SQL sorgusuyla tüm web-aktif ürünleri çeker.
        /// 
        /// AVANTAJLAR:
        /// - StokListesiV2 sayfalı çekim yerine SqlVeriOkuV2 ile tek istek
        /// - Fiyat + stok + barkod + ürün bilgisi tek seferde
        /// - Sadece webe_gonderilecek = 1 ürünler (veri hacmi %80 azalır)
        /// - 60 saniye TTL cache (concurrent isteklerde Mikro'ya tekrar gitmez)
        /// 
        /// HATA DURUMU: Boş liste döner, çağıran servis eski cache'den devam edebilir.
        /// </summary>
        public async Task<List<MikroUnifiedProductDto>> GetUnifiedProductMapAsync(
            int? fiyatListesiNo = null,
            int? depoNo = null,
            CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            // Cache key: fiyatListesiNo bazlı — farklı liste parametresi cache'i geçersiz kılar
            var cacheKeyListNo = fiyatListesiNo ?? 0;

            // Hızlı cache kontrolü — lock almadan önce kontrol et (double-check pattern)
            // ÖNEMLI: Sadece dolu (Count > 0) cache kabul edilir; boş liste cache'lenmez.
            if (_sharedUnifiedProductMap != null &&
                _sharedUnifiedProductMap.Count > 0 &&
                _sharedUnifiedProductMapFiyatListesiNo == cacheKeyListNo &&
                (now - _sharedUnifiedProductMapAtUtc) < UnifiedProductMapCacheTtl)
            {
                _logger.LogInformation(
                    "[MicroService] Birleşik ürün cache HIT. Kayıt: {Count}, Yaş: {AgeSeconds}s",
                    _sharedUnifiedProductMap.Count,
                    (int)(now - _sharedUnifiedProductMapAtUtc).TotalSeconds);
                return new List<MikroUnifiedProductDto>(_sharedUnifiedProductMap);
            }

            await UnifiedProductMapLock.WaitAsync(cancellationToken);
            try
            {
                // Double-check: Lock aldıktan sonra başka thread doldurmuş olabilir
                now = DateTime.UtcNow;
                if (_sharedUnifiedProductMap != null &&
                    _sharedUnifiedProductMap.Count > 0 &&
                    _sharedUnifiedProductMapFiyatListesiNo == cacheKeyListNo &&
                    (now - _sharedUnifiedProductMapAtUtc) < UnifiedProductMapCacheTtl)
                {
                    return new List<MikroUnifiedProductDto>(_sharedUnifiedProductMap);
                }

                // Circuit breaker kontrolü — artık sadece direkt DB için (HTTP değil)
                if (!_mikroDbService.IsConfigured)
                {
                    _logger.LogWarning(
                        "[MicroService] MikroDbService yapılandırılmamış. " +
                        "MikroSettings:SqlConnectionString eksik.");

                    return _sharedUnifiedProductMap != null
                        ? new List<MikroUnifiedProductDto>(_sharedUnifiedProductMap)
                        : new List<MikroUnifiedProductDto>();
                }

                _logger.LogInformation(
                    "[MicroService] Birleşik SQL sorgusu DOĞRUDAN DB'ye gönderiliyor. FiyatListesiNo: {FiyatListesiNo}, DepoNo: {DepoNo}",
                    fiyatListesiNo ?? -1, depoNo ?? -1);

                // ÖN ADIM: Liste 1'deki fiyatları Liste 11'e kopyala (DELETE 11 → INSERT → UPDATE from 1)
                // NEDEN: Liste 1 orijinal Mikro fiyatları, liste 11 web için temiz kopya.
                // SELECT sorguları liste 11'den okur — orijinal veriye dokunulmaz.
                const int webListeNo = 11;
                const int kaynakListeNo = 1;
                var hedefDepo = depoNo ?? 0;
                var (deleted, inserted, updated) = await _mikroDbService.PrepareWebPriceListAsync(
                    webListeNo, kaynakListeNo, hedefDepo, cancellationToken);

                _logger.LogInformation(
                    "[MicroService] Web fiyat listesi hazırlandı (Liste {Kaynak} → Liste {Hedef}). Silinen: {Deleted}, Eklenen: {Inserted}, Güncellenen: {Updated}",
                    kaynakListeNo, webListeNo, deleted, inserted, updated);

                // YENİ AKIŞ: Liste 11'den oku — hazırlanmış fiyatlar
                // NEDEN: SqlVeriOkuV2 → timeout. Direkt conn → <2s
                var products = await _mikroDbService.GetUnifiedProductsAsync(
                    webListeNo, depoNo, cancellationToken);

                _logger.LogInformation(
                    "[MicroService] Birleşik SQL sorgusu tamamlandı. Toplam ürün: {Count}, Fiyat>0: {PriceOk}, Stok>0: {StockOk}",
                    products.Count,
                    products.Count(p => p.Fiyat > 0),
                    products.Count(p => p.StokMiktar > 0));

                // Direkt DB başarılı — circuit breaker sıfırla (HTTP fallback kalıntısı)
                RegisterSqlSuccess();

                // NEDEN: Boş sonuç cache'lenmez — bağlantı hatası veya geçici DB sorunu
                // olabilir. Sonraki istekte tekrar denenmeli.
                if (products.Count > 0)
                {
                    _sharedUnifiedProductMap = products;
                    _sharedUnifiedProductMapAtUtc = DateTime.UtcNow;
                    _sharedUnifiedProductMapFiyatListesiNo = cacheKeyListNo;
                }
                else
                {
                    _logger.LogWarning(
                        "[MicroService] GetUnifiedProductMapAsync: DB sorgusu 0 kayıt döndürdü. " +
                        "Bağlantı string: {ConnStr}",
                        _settings.SqlConnectionString?.Substring(0, Math.Min(60, _settings.SqlConnectionString?.Length ?? 0)) + "...");
                }

                return new List<MikroUnifiedProductDto>(products);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("[MicroService] Birleşik DB sorgusu timeout/iptal.");

                return _sharedUnifiedProductMap != null
                    ? new List<MikroUnifiedProductDto>(_sharedUnifiedProductMap)
                    : new List<MikroUnifiedProductDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MicroService] Birleşik DB sorgusu beklenmeyen hata.");

                return _sharedUnifiedProductMap != null
                    ? new List<MikroUnifiedProductDto>(_sharedUnifiedProductMap)
                    : new List<MikroUnifiedProductDto>();
            }
            finally
            {
                UnifiedProductMapLock.Release();
            }
        }

        /// <summary>
        /// Birleşik SQL sorgusu sonucunu MikroUnifiedProductDto listesine parse eder.
        /// 
        /// NEDEN AYRI METOD: Parse mantığı test edilebilir ve izole kalır.
        /// Mikro response formatı tutarsız olabilir (nested JSON, array-in-object vb.),
        /// mevcut EnumerateSqlRowObjects/EnumerateSqlStockRowObjects yaklaşımı kullanılır.
        /// </summary>
        private List<MikroUnifiedProductDto> ParseUnifiedProductRows(string content)
        {
            var rows = new List<MikroUnifiedProductDto>();

            try
            {
                using var doc = JsonDocument.Parse(content);
                // Mikro SqlVeriOkuV2 response yapısı değişken — deep enumerate ile row objeleri bulunur
                var rowObjects = EnumerateUnifiedRowObjects(doc.RootElement).ToList();
                _logger.LogInformation(
                    "[MicroService] ParseUnifiedProductRows: toplam enumerasyon sayısı: {Count}",
                    rowObjects.Count);

                // Duplikat stok kodlarını önlemek için set — ilk gelen (en güncel hareket) kazanır
                var seenStokKods = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var row in rowObjects)
                {
                    var stokKod = ReadStringFromRow(row, "stokkod", "stok_kod", "StoKod");
                    if (string.IsNullOrWhiteSpace(stokKod))
                        continue;

                    var normalizedSku = stokKod.Trim();

                    // ORDER BY son_hareket_tarihi DESC olduğu için ilk gelen en güncel
                    if (!seenStokKods.Add(normalizedSku))
                        continue;

                    var fiyatRaw = ReadStringFromRow(row, "fiyat", "sfiyat_fiyati", "Fiyat");
                    var stokRaw = ReadStringFromRow(row, "stok_miktar", "StokMiktar", "miktar");
                    var depoNoRaw = ReadStringFromRow(row, "depo_no", "DepoNo");
                    var kdvRaw = ReadStringFromRow(row, "kdv_orani", "KdvOrani");
                    var webFlagRaw = ReadStringFromRow(row, "webe_gonderilecek_fl", "WebeGonderilecekFl");
                    var sonHareketRaw = ReadStringFromRow(row, "son_hareket_tarihi", "SonHareketTarihi");

                    rows.Add(new MikroUnifiedProductDto
                    {
                        StokKod = normalizedSku,
                        StokAd = ReadStringFromRow(row, "stokad", "sto_isim", "StokAd"),
                        Fiyat = ParseDecimalFlexible(fiyatRaw),
                        StokMiktar = ParseDecimalFlexible(stokRaw),
                        DepoNo = int.TryParse(depoNoRaw, out var dno) ? dno : null,
                        Barkod = ReadStringFromRow(row, "barkod", "bar_kodu", "Barkod"),
                        GrupKod = ReadStringFromRow(row, "grup_kod", "sto_grup_kod", "GrupKod"),
                        // NEDEN: AnagrupKod SQL'den dönüyor ama parse edilmiyordu — kategori eşleme bozuktu
                        AnagrupKod = ReadStringFromRow(row, "anagrup_kod", "sto_anagrup_kod", "AnagrupKod"),
                        Birim = ReadStringFromRow(row, "birim", "sto_birim1_ad", "Birim"),
                        KdvOrani = ParseDecimalFlexible(kdvRaw),
                        WebeGonderilecekFl = ParseBoolFlexible(webFlagRaw) ?? true,
                        SonHareketTarihi = DateTime.TryParse(sonHareketRaw, out var sht) ? sht : null
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[MicroService] Birleşik SQL parse hatası.");
            }

            return rows;
        }

        /// <summary>
        /// Birleşik SQL response JSON'ından row objelerini recursive olarak bulur.
        /// NEDEN: Mikro SqlVeriOkuV2 response formatı ortama göre değişebilir
        /// (direkt array, nested object, result wrapper vb.).
        /// Satır tanıma kriteri: hem "stokkod" hem "fiyat" hem "stok_miktar" alanı olan obje.
        /// </summary>
        private static IEnumerable<JsonElement> EnumerateUnifiedRowObjects(JsonElement node)
        {
            if (node.ValueKind == JsonValueKind.Object)
            {
                if (LooksLikeUnifiedRow(node))
                {
                    yield return node;
                    yield break;
                }

                foreach (var prop in node.EnumerateObject())
                {
                    foreach (var child in EnumerateUnifiedRowObjects(prop.Value))
                    {
                        yield return child;
                    }
                }
            }
            else if (node.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in node.EnumerateArray())
                {
                    foreach (var child in EnumerateUnifiedRowObjects(item))
                    {
                        yield return child;
                    }
                }
            }
        }

        /// <summary>
        /// JSON objesinin birleşik sorgu satırı olup olmadığını kontrol eder.
        /// Kriter: stokkod + (fiyat veya stok_miktar) alanlarının varlığı.
        /// </summary>
        private static bool LooksLikeUnifiedRow(JsonElement obj)
        {
            if (obj.ValueKind != JsonValueKind.Object)
                return false;

            var hasStokkod = TryGetPropertyIgnoreCase(obj, "stokkod", out _) ||
                             TryGetPropertyIgnoreCase(obj, "stok_kod", out _);

            // Birleşik sorgu hem fiyat hem stok içerir — en az biri varsa tanırız
            var hasFiyat = TryGetPropertyIgnoreCase(obj, "fiyat", out _) ||
                           TryGetPropertyIgnoreCase(obj, "sfiyat_fiyati", out _);

            var hasStokMiktar = TryGetPropertyIgnoreCase(obj, "stok_miktar", out _) ||
                                TryGetPropertyIgnoreCase(obj, "StokMiktar", out _);

            return hasStokkod && (hasFiyat || hasStokMiktar);
        }
    }

    // ==================== MIKRO AUTH WRAPPER DTO ====================
    
    /// <summary>
    /// MikroAPI V2 için authentication wrapper sınıfı.
    /// Tüm V2 endpoint'leri request body'de bu yapıyı bekler.
    /// Mikro objesi içinde auth + şirket/şube context alanları taşınır.
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

        /// <summary>Firma numarası (genellikle 0)</summary>
        public int FirmaNo { get; set; }

        /// <summary>Şube numarası (genellikle 0)</summary>
        public int SubeNo { get; set; }
    }
}
