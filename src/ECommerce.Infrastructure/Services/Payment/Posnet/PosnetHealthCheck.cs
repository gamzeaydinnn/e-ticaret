// ═══════════════════════════════════════════════════════════════════════════════════════════════
// POSNET SAĞLIK KONTROLÜ (HEALTH CHECK) SERVİSİ
// POSNET bağlantısı ve konfigürasyon durumunu kontrol eder
// Production monitoring için kritik öneme sahiptir
// 
// KULLANIM ALANLARI:
// - Kubernetes liveness/readiness probe
// - Load balancer health check
// - Monitoring dashboard (Prometheus, Grafana)
// - Otomatik alert sistemi
// ═══════════════════════════════════════════════════════════════════════════════════════════════

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ECommerce.Infrastructure.Config;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ECommerce.Infrastructure.Services.Payment.Posnet
{
    /// <summary>
    /// POSNET sağlık durumu
    /// </summary>
    public class PosnetHealthStatus
    {
        public bool IsHealthy { get; set; }
        public string Status { get; set; } = "Unknown";
        public TimeSpan ResponseTime { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
        public PosnetEnvironmentInfo? EnvironmentInfo { get; set; }
    }

    /// <summary>
    /// POSNET ortam bilgisi
    /// </summary>
    public class PosnetEnvironmentInfo
    {
        public string Environment { get; set; } = "Unknown";
        public string ServiceUrl { get; set; } = string.Empty;
        public bool Is3DSecureEnabled { get; set; }
        public bool IsWorldPointEnabled { get; set; }
        public bool IsMockModeEnabled { get; set; }
    }

    /// <summary>
    /// POSNET Health Check implementasyonu
    /// ASP.NET Core Health Check framework ile entegre
    /// </summary>
    public class PosnetHealthCheck : IHealthCheck
    {
        private readonly PaymentSettings _settings;
        private readonly HttpClient _httpClient;
        private readonly ILogger<PosnetHealthCheck> _logger;

        // Son başarılı kontrol cache (gereksiz API çağrısını önler)
        private static PosnetHealthStatus? _cachedStatus;
        private static DateTime _cacheExpiry = DateTime.MinValue;
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(1);

        public PosnetHealthCheck(
            IOptions<PaymentSettings> options,
            IHttpClientFactory httpClientFactory,
            ILogger<PosnetHealthCheck> logger)
        {
            _settings = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _httpClient = httpClientFactory.CreateClient("PosnetHealthCheck");
            _httpClient.Timeout = TimeSpan.FromSeconds(10); // Health check timeout
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Health check ana metodu
        /// </summary>
        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var status = await GetHealthStatusAsync(cancellationToken);

                var data = new Dictionary<string, object>
                {
                    ["environment"] = status.EnvironmentInfo?.Environment ?? "Unknown",
                    ["responseTime"] = status.ResponseTime.TotalMilliseconds,
                    ["checkedAt"] = status.CheckedAt,
                    ["is3DSecureEnabled"] = status.EnvironmentInfo?.Is3DSecureEnabled ?? false,
                    ["isMockMode"] = status.EnvironmentInfo?.IsMockModeEnabled ?? false
                };

                if (status.IsHealthy)
                {
                    return HealthCheckResult.Healthy(
                        $"POSNET bağlantısı aktif ({status.ResponseTime.TotalMilliseconds:F0}ms)",
                        data);
                }
                else
                {
                    return HealthCheckResult.Unhealthy(
                        status.ErrorMessage ?? "POSNET bağlantısı başarısız",
                        data: data);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[POSNET-HEALTH] Health check exception");

                return HealthCheckResult.Unhealthy(
                    $"POSNET health check hatası: {ex.Message}",
                    ex);
            }
        }

        /// <summary>
        /// Detaylı sağlık durumu alır (cache destekli)
        /// </summary>
        public async Task<PosnetHealthStatus> GetHealthStatusAsync(CancellationToken cancellationToken = default)
        {
            // Cache kontrolü
            if (_cachedStatus != null && DateTime.UtcNow < _cacheExpiry)
            {
                _logger.LogDebug("[POSNET-HEALTH] Cache'den döndürülüyor");
                return _cachedStatus;
            }

            var status = new PosnetHealthStatus
            {
                CheckedAt = DateTime.UtcNow,
                EnvironmentInfo = GetEnvironmentInfo()
            };

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                // Konfigürasyon kontrolü
                if (!ValidateConfiguration(status))
                {
                    return status;
                }

                // POSNET endpoint'ine bağlantı kontrolü
                var serviceUrl = _settings.PosnetXmlServiceUrl;
                
                if (_settings.PosnetIsTestEnvironment)
                {
                    // Test ortamında sadece endpoint erişilebilirliğini kontrol et
                    // Gerçek API çağrısı yapmadan HEAD request
                    using var request = new HttpRequestMessage(HttpMethod.Head, serviceUrl);
                    
                    try
                    {
                        var response = await _httpClient.SendAsync(request, cancellationToken);
                        
                        stopwatch.Stop();
                        status.ResponseTime = stopwatch.Elapsed;

                        // 4xx veya 5xx değilse sağlıklı kabul et
                        // POSNET genellikle 405 Method Not Allowed döner HEAD için, bu normaldir
                        status.IsHealthy = (int)response.StatusCode < 500;
                        status.Status = status.IsHealthy ? "Healthy" : "Unhealthy";

                        if (!status.IsHealthy)
                        {
                            status.ErrorMessage = $"POSNET endpoint yanıt kodu: {(int)response.StatusCode}";
                        }
                    }
                    catch (HttpRequestException httpEx)
                    {
                        stopwatch.Stop();
                        status.ResponseTime = stopwatch.Elapsed;
                        status.IsHealthy = false;
                        status.Status = "Unhealthy";
                        status.ErrorMessage = $"POSNET endpoint erişilemedi: {httpEx.Message}";
                    }
                }
                else
                {
                    // Production'da basit bağlantı kontrolü
                    // NOT: Gerçek API çağrısı yapmıyoruz, sadece TCP bağlantısı kontrol ediliyor
                    using var request = new HttpRequestMessage(HttpMethod.Options, serviceUrl);
                    
                    try
                    {
                        var response = await _httpClient.SendAsync(request, cancellationToken);
                        
                        stopwatch.Stop();
                        status.ResponseTime = stopwatch.Elapsed;
                        
                        // Herhangi bir yanıt geldiyse endpoint erişilebilir
                        status.IsHealthy = true;
                        status.Status = "Healthy";
                    }
                    catch (HttpRequestException)
                    {
                        // HTTP hatası olsa bile bağlantı var mı kontrol et
                        status.IsHealthy = await CheckTcpConnectivityAsync(serviceUrl, cancellationToken);
                        status.Status = status.IsHealthy ? "Healthy" : "Unhealthy";
                        
                        if (!status.IsHealthy)
                        {
                            status.ErrorMessage = "POSNET endpoint'ine TCP bağlantısı kurulamadı";
                        }
                        
                        stopwatch.Stop();
                        status.ResponseTime = stopwatch.Elapsed;
                    }
                }

                // Başarılı sonucu cache'le
                if (status.IsHealthy)
                {
                    _cachedStatus = status;
                    _cacheExpiry = DateTime.UtcNow.Add(CacheDuration);
                }

                _logger.LogInformation(
                    "[POSNET-HEALTH] Durum: {Status} | Süre: {Duration}ms | Ortam: {Environment}",
                    status.Status,
                    status.ResponseTime.TotalMilliseconds,
                    status.EnvironmentInfo?.Environment);

                return status;
            }
            catch (OperationCanceledException)
            {
                status.IsHealthy = false;
                status.Status = "Timeout";
                status.ErrorMessage = "Health check zaman aşımına uğradı";
                status.ResponseTime = stopwatch.Elapsed;
                return status;
            }
            catch (Exception ex)
            {
                status.IsHealthy = false;
                status.Status = "Error";
                status.ErrorMessage = ex.Message;
                status.ResponseTime = stopwatch.Elapsed;

                _logger.LogError(ex, "[POSNET-HEALTH] Beklenmeyen hata");

                return status;
            }
        }

        /// <summary>
        /// Konfigürasyon doğrulaması
        /// </summary>
        private bool ValidateConfiguration(PosnetHealthStatus status)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(_settings.PosnetMerchantId))
                errors.Add("MerchantId eksik");

            if (string.IsNullOrWhiteSpace(_settings.PosnetTerminalId))
                errors.Add("TerminalId eksik");

            if (string.IsNullOrWhiteSpace(_settings.PosnetId))
                errors.Add("PosnetId eksik");

            if (string.IsNullOrWhiteSpace(_settings.PosnetXmlServiceUrl))
                errors.Add("XML Service URL eksik");

            if (errors.Count > 0)
            {
                status.IsHealthy = false;
                status.Status = "ConfigurationError";
                status.ErrorMessage = $"Konfigürasyon hatası: {string.Join(", ", errors)}";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Ortam bilgisi oluşturur
        /// </summary>
        private PosnetEnvironmentInfo GetEnvironmentInfo()
        {
            return new PosnetEnvironmentInfo
            {
                Environment = _settings.PosnetIsTestEnvironment ? "Test (Sandbox)" : "Production",
                ServiceUrl = MaskUrl(_settings.PosnetXmlServiceUrl),
                Is3DSecureEnabled = !string.IsNullOrEmpty(_settings.Posnet3DServiceUrl),
                IsWorldPointEnabled = _settings.PosnetWorldPointEnabled,
                IsMockModeEnabled = _settings.PosnetIsTestEnvironment // Mock genellikle test ortamında aktif
            };
        }

        /// <summary>
        /// URL'yi loglamak için maskeler
        /// </summary>
        private static string MaskUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
                return "N/A";

            try
            {
                var uri = new Uri(url);
                return $"{uri.Scheme}://{uri.Host}/...";
            }
            catch
            {
                return "Invalid URL";
            }
        }

        /// <summary>
        /// TCP bağlantı kontrolü
        /// </summary>
        private async Task<bool> CheckTcpConnectivityAsync(string url, CancellationToken cancellationToken)
        {
            try
            {
                var uri = new Uri(url);
                using var tcpClient = new System.Net.Sockets.TcpClient();
                
                var connectTask = tcpClient.ConnectAsync(uri.Host, uri.Port);
                var timeoutTask = Task.Delay(5000, cancellationToken);
                
                var completedTask = await Task.WhenAny(connectTask, timeoutTask);
                
                return completedTask == connectTask && tcpClient.Connected;
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>
    /// POSNET Health Check extension metotları
    /// </summary>
    public static class PosnetHealthCheckExtensions
    {
        /// <summary>
        /// POSNET health check'i health check builder'a ekler
        /// </summary>
        public static IHealthChecksBuilder AddPosnetHealthCheck(
            this IHealthChecksBuilder builder,
            string name = "posnet",
            HealthStatus? failureStatus = null,
            IEnumerable<string>? tags = null)
        {
            return builder.AddCheck<PosnetHealthCheck>(
                name,
                failureStatus ?? HealthStatus.Degraded,
                tags ?? new[] { "payment", "posnet", "external" });
        }
    }
}
