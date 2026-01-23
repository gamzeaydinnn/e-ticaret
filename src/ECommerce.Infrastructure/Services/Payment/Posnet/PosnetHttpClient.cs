// ═══════════════════════════════════════════════════════════════════════════════════════════════
// POSNET HTTP CLIENT
// Yapı Kredi POSNET XML API ile HTTP iletişimi sağlayan servis
// Dokümantasyon: POSNET XML Servisleri Entegrasyon Dokümanı v2.1.1.3
// ═══════════════════════════════════════════════════════════════════════════════════════════════
// NEDEN BU YAPIYI SEÇTİK?
// 1. IHttpClientFactory pattern - HttpClient lifecycle yönetimi, connection pooling
// 2. Retry policy (Polly) - Geçici hatalarda otomatik yeniden deneme
// 3. Timeout yönetimi - Asılı kalan requestler için güvenlik
// 4. Structured logging - Her request/response loglanır
// 5. Header management - POSNET zorunlu header'ları otomatik eklenir
// 6. XID-based Correlation - İşlem takibi için sipariş numarası bazlı tracking
// ═══════════════════════════════════════════════════════════════════════════════════════════════

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ECommerce.Infrastructure.Config;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ECommerce.Infrastructure.Services.Payment.Posnet
{
    // ═══════════════════════════════════════════════════════════════════════════════
    // POSNET REQUEST OPTIONS
    // Her istek için özelleştirilebilir ayarlar - XID bazlı correlation tracking için
    // ═══════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// POSNET HTTP request için opsiyonel ayarlar
    /// XID bazlı correlation tracking ve özel header'lar için kullanılır
    /// </summary>
    public class PosnetRequestOptions
    {
        /// <summary>
        /// İşlem referans numarası (XID) - Correlation tracking için kritik
        /// Bu değer X-CORRELATION-ID header'ına eklenir ve log'larda kullanılır
        /// Null ise otomatik GUID üretilir
        /// </summary>
        public string? Xid { get; set; }

        /// <summary>
        /// Sipariş ID - İşlem takibi için
        /// </summary>
        public int? OrderId { get; set; }

        /// <summary>
        /// Özel header'lar - İhtiyaç halinde ek header eklemek için
        /// </summary>
        public Dictionary<string, string>? CustomHeaders { get; set; }

        /// <summary>
        /// İşlem açıklaması - Loglama için
        /// </summary>
        public string? OperationDescription { get; set; }

        /// <summary>
        /// Boş options factory
        /// </summary>
        public static PosnetRequestOptions Empty => new();

        /// <summary>
        /// XID ile options oluşturur
        /// </summary>
        public static PosnetRequestOptions WithXid(string xid) => new() { Xid = xid };

        /// <summary>
        /// OrderId ile options oluşturur
        /// </summary>
        public static PosnetRequestOptions WithOrderId(int orderId) => new() { OrderId = orderId };

        /// <summary>
        /// XID ve OrderId ile options oluşturur
        /// </summary>
        public static PosnetRequestOptions Create(string? xid, int? orderId, string? description = null)
        {
            return new PosnetRequestOptions
            {
                Xid = xid,
                OrderId = orderId,
                OperationDescription = description
            };
        }
    }

    /// <summary>
    /// POSNET HTTP client interface
    /// Dependency Injection ve Unit Test için
    /// </summary>
    public interface IPosnetHttpClient
    {
        /// <summary>
        /// XML request'i POSNET API'ye gönderir ve yanıtı alır
        /// </summary>
        /// <param name="xml">Gönderilecek XML</param>
        /// <param name="cancellationToken">İptal token'ı</param>
        /// <returns>POSNET yanıt XML'i</returns>
        Task<PosnetHttpResponse> SendAsync(string xml, CancellationToken cancellationToken = default);

        /// <summary>
        /// XML request'i POSNET API'ye özel options ile gönderir
        /// XID bazlı correlation tracking için bu metodu kullanın
        /// </summary>
        /// <param name="xml">Gönderilecek XML</param>
        /// <param name="options">Request options (XID, OrderId, CustomHeaders)</param>
        /// <param name="cancellationToken">İptal token'ı</param>
        /// <returns>POSNET yanıt XML'i</returns>
        Task<PosnetHttpResponse> SendAsync(string xml, PosnetRequestOptions options, 
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// 3D Secure endpoint'ine XML gönderir
        /// </summary>
        /// <param name="xml">Gönderilecek XML</param>
        /// <param name="cancellationToken">İptal token'ı</param>
        /// <returns>POSNET yanıt XML'i</returns>
        Task<PosnetHttpResponse> Send3DSecureAsync(string xml, CancellationToken cancellationToken = default);

        /// <summary>
        /// 3D Secure endpoint'ine XML gönderir - XID destekli
        /// </summary>
        /// <param name="xml">Gönderilecek XML</param>
        /// <param name="options">Request options (XID, OrderId, CustomHeaders)</param>
        /// <param name="cancellationToken">İptal token'ı</param>
        /// <returns>POSNET yanıt XML'i</returns>
        Task<PosnetHttpResponse> Send3DSecureAsync(string xml, PosnetRequestOptions options,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// POSNET HTTP yanıt modeli
    /// HTTP durum kodu ve XML içeriği taşır
    /// </summary>
    public class PosnetHttpResponse
    {
        /// <summary>İstek başarılı mı?</summary>
        public bool IsSuccess { get; init; }
        
        /// <summary>HTTP durum kodu</summary>
        public HttpStatusCode StatusCode { get; init; }
        
        /// <summary>Yanıt XML'i</summary>
        public string? ResponseXml { get; init; }
        
        /// <summary>Hata mesajı (başarısız ise)</summary>
        public string? ErrorMessage { get; init; }
        
        /// <summary>İşlem süresi (ms)</summary>
        public long ElapsedMilliseconds { get; init; }
        
        /// <summary>Exception (varsa)</summary>
        public Exception? Exception { get; init; }

        /// <summary>
        /// Correlation ID - İşlem takibi için
        /// XID veya otomatik oluşturulan GUID
        /// </summary>
        public string? CorrelationId { get; init; }

        /// <summary>
        /// XID - İşlem referans numarası (varsa)
        /// </summary>
        public string? Xid { get; init; }

        /// <summary>Başarılı yanıt factory</summary>
        public static PosnetHttpResponse Success(string xml, HttpStatusCode statusCode, long elapsedMs, 
            string? correlationId = null, string? xid = null)
        {
            return new PosnetHttpResponse
            {
                IsSuccess = true,
                StatusCode = statusCode,
                ResponseXml = xml,
                ElapsedMilliseconds = elapsedMs,
                CorrelationId = correlationId,
                Xid = xid
            };
        }

        /// <summary>Başarısız yanıt factory</summary>
        public static PosnetHttpResponse Failure(string error, HttpStatusCode statusCode, long elapsedMs, 
            Exception? ex = null, string? correlationId = null, string? xid = null)
        {
            return new PosnetHttpResponse
            {
                IsSuccess = false,
                StatusCode = statusCode,
                ErrorMessage = error,
                ElapsedMilliseconds = elapsedMs,
                Exception = ex,
                CorrelationId = correlationId,
                Xid = xid
            };
        }
    }

    /// <summary>
    /// POSNET HTTP Client implementasyonu
    /// POSNET XML API ile güvenli HTTP iletişimi sağlar
    /// </summary>
    public class PosnetHttpClient : IPosnetHttpClient
    {
        private readonly HttpClient _httpClient;
        private readonly PaymentSettings _settings;
        private readonly ILogger<PosnetHttpClient> _logger;
        private readonly IPosnetXmlBuilder _xmlBuilder;

        // ═══════════════════════════════════════════════════════════════════════
        // CONSTANTS
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>Content-Type header değeri - POSNET zorunlu</summary>
        private const string CONTENT_TYPE = "application/x-www-form-urlencoded";
        
        /// <summary>Charset - UTF-8 zorunlu</summary>
        private const string CHARSET = "utf-8";
        
        /// <summary>POST parametre adı</summary>
        private const string XML_PARAM = "xmldata";

        public PosnetHttpClient(
            HttpClient httpClient,
            IOptions<PaymentSettings> settings,
            ILogger<PosnetHttpClient> logger,
            IPosnetXmlBuilder xmlBuilder)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _xmlBuilder = xmlBuilder ?? throw new ArgumentNullException(nameof(xmlBuilder));

            ConfigureHttpClient();
        }

        /// <summary>
        /// HttpClient'ı POSNET gereksinimleri için yapılandırır
        /// </summary>
        private void ConfigureHttpClient()
        {
            // Timeout ayarı
            _httpClient.Timeout = TimeSpan.FromSeconds(_settings.PosnetTimeoutSeconds > 0 
                ? _settings.PosnetTimeoutSeconds 
                : 60);

            // Default headers
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));
        }

        // ═══════════════════════════════════════════════════════════════════════
        // SEND - Ana XML API isteği
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// XML request'i POSNET API'ye gönderir (basit versiyon)
        /// XID yoksa otomatik GUID oluşturulur
        /// </summary>
        public async Task<PosnetHttpResponse> SendAsync(string xml, CancellationToken cancellationToken = default)
        {
            // Options olmadan çağrıldığında boş options kullan
            return await SendAsync(xml, PosnetRequestOptions.Empty, cancellationToken);
        }

        /// <summary>
        /// XML request'i POSNET API'ye XID destekli olarak gönderir
        /// Bu metod tercih edilmeli - işlem takibi için XID kritik
        /// </summary>
        public async Task<PosnetHttpResponse> SendAsync(string xml, PosnetRequestOptions options, 
            CancellationToken cancellationToken = default)
        {
            return await SendToEndpointAsync(
                _settings.PosnetXmlServiceUrl, 
                xml, 
                options,
                cancellationToken);
        }

        /// <summary>
        /// 3D Secure endpoint'ine XML gönderir (basit versiyon)
        /// </summary>
        public async Task<PosnetHttpResponse> Send3DSecureAsync(string xml, CancellationToken cancellationToken = default)
        {
            return await Send3DSecureAsync(xml, PosnetRequestOptions.Empty, cancellationToken);
        }

        /// <summary>
        /// 3D Secure endpoint'ine XML gönderir - XID destekli
        /// </summary>
        public async Task<PosnetHttpResponse> Send3DSecureAsync(string xml, PosnetRequestOptions options,
            CancellationToken cancellationToken = default)
        {
            return await SendToEndpointAsync(
                _settings.Posnet3DServiceUrl, 
                xml, 
                options,
                cancellationToken);
        }

        // ═══════════════════════════════════════════════════════════════════════
        // CORE SEND METHOD
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Belirtilen endpoint'e XML gönderir
        /// Retry, timeout ve hata yönetimi dahil
        /// XID bazlı correlation tracking destekli
        /// </summary>
        private async Task<PosnetHttpResponse> SendToEndpointAsync(
            string endpoint, 
            string xml, 
            PosnetRequestOptions options,
            CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();
            
            // Correlation ID belirleme:
            // 1. Öncelik: Options'dan gelen XID
            // 2. Fallback: Otomatik GUID
            var xid = options?.Xid;
            var correlationId = !string.IsNullOrWhiteSpace(xid) 
                ? xid 
                : Guid.NewGuid().ToString("N")[..16];
            
            var orderId = options?.OrderId;
            var operationDesc = options?.OperationDescription ?? "POSNET Request";

            try
            {
                // Validasyon
                if (string.IsNullOrWhiteSpace(xml))
                {
                    throw new ArgumentException("XML boş olamaz", nameof(xml));
                }

                if (string.IsNullOrWhiteSpace(endpoint))
                {
                    throw new InvalidOperationException("POSNET API URL yapılandırılmamış");
                }

                // XML'i URL encode et
                var encodedXml = _xmlBuilder.EncodeForPost(xml);
                var postData = $"{XML_PARAM}={encodedXml}";

                // Log request (hassas veriler maskelenmiş)
                _logger.LogInformation(
                    "[POSNET] {Operation} - Request gönderiliyor. CorrelationId: {CorrelationId}, " +
                    "XID: {Xid}, OrderId: {OrderId}, Endpoint: {Endpoint}",
                    operationDesc, correlationId, xid ?? "N/A", orderId?.ToString() ?? "N/A", MaskUrl(endpoint));

                // HTTP request oluştur
                using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
                request.Content = new StringContent(postData, Encoding.UTF8, CONTENT_TYPE);
                
                // POSNET zorunlu ve önerilen header'ları ekle
                AddPosnetHeaders(request, correlationId, xid, options?.CustomHeaders);

                // Request gönder
                using var response = await _httpClient.SendAsync(request, cancellationToken);
                
                // Response oku
                var responseXml = await response.Content.ReadAsStringAsync(cancellationToken);
                
                stopwatch.Stop();

                // Log response
                _logger.LogInformation(
                    "[POSNET] {Operation} - Response alındı. CorrelationId: {CorrelationId}, " +
                    "StatusCode: {StatusCode}, ElapsedMs: {ElapsedMs}",
                    operationDesc, correlationId, (int)response.StatusCode, stopwatch.ElapsedMilliseconds);

                // HTTP başarılı mı kontrol et
                if (!response.IsSuccessStatusCode)
                {
                    // 500 hatası için tam response'u logla
                    _logger.LogError(
                        "[POSNET] HTTP {StatusCode} HATASI. CorrelationId: {CorrelationId}\nFull Response:\n{Response}",
                        (int)response.StatusCode, correlationId, responseXml);

                    return PosnetHttpResponse.Failure(
                        $"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}",
                        response.StatusCode,
                        stopwatch.ElapsedMilliseconds,
                        correlationId: correlationId,
                        xid: xid);
                }

                // Başarılı yanıt
                return PosnetHttpResponse.Success(
                    responseXml, 
                    response.StatusCode, 
                    stopwatch.ElapsedMilliseconds,
                    correlationId,
                    xid);
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException || !cancellationToken.IsCancellationRequested)
            {
                // Timeout
                stopwatch.Stop();
                _logger.LogError(ex,
                    "[POSNET] Timeout. CorrelationId: {CorrelationId}, XID: {Xid}, ElapsedMs: {ElapsedMs}",
                    correlationId, xid ?? "N/A", stopwatch.ElapsedMilliseconds);

                return PosnetHttpResponse.Failure(
                    "POSNET API yanıt vermedi (timeout)",
                    HttpStatusCode.RequestTimeout,
                    stopwatch.ElapsedMilliseconds,
                    ex,
                    correlationId,
                    xid);
            }
            catch (HttpRequestException ex)
            {
                // Network hatası
                stopwatch.Stop();
                _logger.LogError(ex,
                    "[POSNET] Network hatası. CorrelationId: {CorrelationId}, XID: {Xid}, Message: {Message}",
                    correlationId, xid ?? "N/A", ex.Message);

                return PosnetHttpResponse.Failure(
                    $"Network hatası: {ex.Message}",
                    HttpStatusCode.ServiceUnavailable,
                    stopwatch.ElapsedMilliseconds,
                    ex,
                    correlationId,
                    xid);
            }
            catch (OperationCanceledException ex)
            {
                // İşlem iptal edildi
                stopwatch.Stop();
                _logger.LogWarning(ex,
                    "[POSNET] İşlem iptal edildi. CorrelationId: {CorrelationId}, XID: {Xid}",
                    correlationId, xid ?? "N/A");

                return PosnetHttpResponse.Failure(
                    "İşlem iptal edildi",
                    HttpStatusCode.RequestTimeout,
                    stopwatch.ElapsedMilliseconds,
                    ex,
                    correlationId,
                    xid);
            }
            catch (Exception ex)
            {
                // Beklenmeyen hata
                stopwatch.Stop();
                _logger.LogError(ex,
                    "[POSNET] Beklenmeyen hata. CorrelationId: {CorrelationId}, XID: {Xid}",
                    correlationId, xid ?? "N/A");

                return PosnetHttpResponse.Failure(
                    $"Beklenmeyen hata: {ex.Message}",
                    HttpStatusCode.InternalServerError,
                    stopwatch.ElapsedMilliseconds,
                    ex,
                    correlationId,
                    xid);
            }
        }

        // ═══════════════════════════════════════════════════════════════════════
        // HEADER MANAGEMENT
        // POSNET Dokümantasyonu v2.1.1.3'e göre zorunlu ve önerilen header'lar
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// POSNET zorunlu ve önerilen header'ları request'e ekler
        /// 
        /// Dokümantasyona göre eklenen header'lar:
        /// - X-MERCHANT-ID: Üye işyeri numarası (MID)
        /// - X-TERMINAL-ID: Terminal ID (TID) 
        /// - X-POSNET-ID: POSNET ID
        /// - X-CORRELATION-ID: İşlem takibi için benzersiz ID (XID veya GUID)
        /// 
        /// Ek olarak özel header'lar da eklenebilir (CustomHeaders)
        /// </summary>
        /// <param name="request">HTTP request</param>
        /// <param name="correlationId">Correlation ID (XID veya otomatik GUID)</param>
        /// <param name="xid">XID - İşlem referans numarası (opsiyonel)</param>
        /// <param name="customHeaders">Özel header'lar (opsiyonel)</param>
        private void AddPosnetHeaders(HttpRequestMessage request, string correlationId, 
            string? xid = null, Dictionary<string, string>? customHeaders = null)
        {
            // ═══════════════════════════════════════════════════════════════
            // ZORUNLU HEADER'LAR (POSNET Doküman Sayfa 12)
            // Bu header'lar banka tarafından işlem takibi için kullanılır
            // ═══════════════════════════════════════════════════════════════

            // X-MERCHANT-ID: Üye işyeri numarası
            if (!string.IsNullOrWhiteSpace(_settings.PosnetMerchantId))
            {
                request.Headers.TryAddWithoutValidation("X-MERCHANT-ID", _settings.PosnetMerchantId);
            }
            else
            {
                _logger.LogWarning("[POSNET-HEADERS] X-MERCHANT-ID header'ı eklenemedi - PosnetMerchantId yapılandırılmamış");
            }

            // X-TERMINAL-ID: Terminal ID
            if (!string.IsNullOrWhiteSpace(_settings.PosnetTerminalId))
            {
                request.Headers.TryAddWithoutValidation("X-TERMINAL-ID", _settings.PosnetTerminalId);
            }
            else
            {
                _logger.LogWarning("[POSNET-HEADERS] X-TERMINAL-ID header'ı eklenemedi - PosnetTerminalId yapılandırılmamış");
            }

            // X-POSNET-ID: POSNET ID
            if (!string.IsNullOrWhiteSpace(_settings.PosnetId))
            {
                request.Headers.TryAddWithoutValidation("X-POSNET-ID", _settings.PosnetId);
            }

            // ═══════════════════════════════════════════════════════════════
            // X-CORRELATION-ID: İşlem takibi için kritik
            // XID varsa onu kullan, yoksa otomatik oluşturulan correlationId
            // Bu header sayesinde log'larda ve banka tarafında işlem takibi yapılabilir
            // ═══════════════════════════════════════════════════════════════
            request.Headers.TryAddWithoutValidation("X-CORRELATION-ID", correlationId);

            // XID ayrıca X-XID header'ı olarak da eklenebilir (opsiyonel)
            if (!string.IsNullOrWhiteSpace(xid))
            {
                request.Headers.TryAddWithoutValidation("X-XID", xid);
            }

            // ═══════════════════════════════════════════════════════════════
            // EK HEADER'LAR
            // Request timestamp ve User-Agent gibi standart header'lar
            // ═══════════════════════════════════════════════════════════════

            // Request timestamp (ISO 8601 format)
            request.Headers.TryAddWithoutValidation("X-REQUEST-TIMESTAMP", 
                DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));

            // User-Agent (uygulama bilgisi)
            if (!request.Headers.Contains("User-Agent"))
            {
                request.Headers.TryAddWithoutValidation("User-Agent", 
                    "ECommerce-POSNET-Client/1.0");
            }

            // ═══════════════════════════════════════════════════════════════
            // ÖZEL HEADER'LAR (CustomHeaders)
            // İhtiyaç halinde ek header eklemek için
            // ═══════════════════════════════════════════════════════════════
            if (customHeaders != null && customHeaders.Count > 0)
            {
                foreach (var header in customHeaders)
                {
                    if (!string.IsNullOrWhiteSpace(header.Key) && header.Value != null)
                    {
                        // Mevcut header'ı ezme, sadece yoksa ekle
                        if (!request.Headers.Contains(header.Key))
                        {
                            request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                        }
                        else
                        {
                            _logger.LogDebug("[POSNET-HEADERS] Custom header atlandı (zaten mevcut): {HeaderKey}", 
                                header.Key);
                        }
                    }
                }
            }

            // Debug log - eklenen header'lar
            _logger.LogDebug("[POSNET-HEADERS] Header'lar eklendi - CorrelationId: {CorrelationId}, " +
                "XID: {Xid}, MerchantId: {MerchantId}, TerminalId: {TerminalId}",
                correlationId,
                xid ?? "N/A",
                MaskValue(_settings.PosnetMerchantId),
                MaskValue(_settings.PosnetTerminalId));
        }

        // ═══════════════════════════════════════════════════════════════════════
        // HELPER METHODS
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// URL'yi log için maskeler
        /// Hassas bilgiler görünmez
        /// </summary>
        private static string MaskUrl(string url)
        {
            if (string.IsNullOrEmpty(url)) return "[empty]";
            
            try
            {
                var uri = new Uri(url);
                return $"{uri.Scheme}://{uri.Host}/***";
            }
            catch
            {
                return "[invalid-url]";
            }
        }

        /// <summary>
        /// Hassas değeri log için maskeler
        /// Örnek: "12345678" -> "1234****"
        /// </summary>
        private static string MaskValue(string? value, int visibleChars = 4)
        {
            if (string.IsNullOrEmpty(value)) return "[not-set]";
            if (value.Length <= visibleChars) return new string('*', value.Length);
            return value[..visibleChars] + "****";
        }

        /// <summary>
        /// Log için metni kısaltır
        /// </summary>
        private static string TruncateForLog(string? text, int maxLength = 200)
        {
            if (string.IsNullOrEmpty(text)) return "[empty]";
            if (text.Length <= maxLength) return text;
            return text[..maxLength] + "...";
        }
    }
}
