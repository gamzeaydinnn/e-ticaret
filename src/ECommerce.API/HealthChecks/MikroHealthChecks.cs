using ECommerce.Infrastructure.Config;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ECommerce.API.HealthChecks
{
    /// <summary>
    /// Mikro ERP SQL Server bağlantı sağlık kontrolü.
    /// 
    /// NEDEN: HotPoll ve UnifiedSync Mikro DB'ye direkt SQL bağlantısı kullanıyor.
    /// Bu bağlantı kesilirse sync tamamen durur. Health check ile durumu
    /// /health endpoint'i üzerinden izleniyor.
    /// 
    /// STRATEJİ:
    /// - SELECT 1 ile minimal sorgu (DB yükü neredeyse sıfır)
    /// - 5sn timeout — yavaş yanıt "Degraded" olarak raporlanır
    /// - Bağlantı yoksa (yapılandırılmamışsa) "Degraded" döner (crash değil)
    /// </summary>
    public class MikroSqlHealthCheck : IHealthCheck
    {
        private readonly MikroSettings _settings;
        private readonly ILogger<MikroSqlHealthCheck> _logger;
        private static readonly TimeSpan CheckTimeout = TimeSpan.FromSeconds(5);

        public MikroSqlHealthCheck(
            IOptions<MikroSettings> settings,
            ILogger<MikroSqlHealthCheck> logger)
        {
            _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            // SQL bağlantı string'i yapılandırılmamışsa degrade döner
            if (string.IsNullOrWhiteSpace(_settings.SqlConnectionString))
            {
                return HealthCheckResult.Degraded(
                    "Mikro SQL bağlantı string'i yapılandırılmamış.",
                    data: new Dictionary<string, object>
                    {
                        ["configured"] = false
                    });
            }

            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(CheckTimeout);

                await using var connection = new SqlConnection(_settings.SqlConnectionString);
                await connection.OpenAsync(cts.Token);

                await using var command = connection.CreateCommand();
                command.CommandText = "SELECT 1";
                command.CommandTimeout = 5;
                await command.ExecuteScalarAsync(cts.Token);

                return HealthCheckResult.Healthy(
                    "Mikro SQL bağlantısı aktif.",
                    data: new Dictionary<string, object>
                    {
                        ["configured"] = true,
                        ["server"] = MaskConnectionString(_settings.SqlConnectionString)
                    });
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("[HealthCheck] Mikro SQL bağlantı kontrolü zaman aşımına uğradı.");
                return HealthCheckResult.Degraded("Mikro SQL bağlantısı zaman aşımına uğradı (5sn).");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[HealthCheck] Mikro SQL bağlantı kontrolü başarısız.");
                return HealthCheckResult.Unhealthy(
                    "Mikro SQL bağlantısı başarısız.",
                    exception: ex,
                    data: new Dictionary<string, object>
                    {
                        ["error"] = ex.Message
                    });
            }
        }

        /// <summary>
        /// Bağlantı string'inden hassas bilgileri maskeler (password gizle).
        /// </summary>
        private static string MaskConnectionString(string connectionString)
        {
            try
            {
                var builder = new SqlConnectionStringBuilder(connectionString);
                if (!string.IsNullOrEmpty(builder.Password))
                    builder.Password = "***";
                return builder.ConnectionString;
            }
            catch
            {
                return "***masked***";
            }
        }
    }

    /// <summary>
    /// Mikro ERP HTTP API sağlık kontrolü.
    /// 
    /// NEDEN: Sipariş push (SiparisKaydetV2) ve bazı fallback operasyonları
    /// Mikro HTTP API'sine bağlı. API erişilemezse bu işlemler başarısız olur.
    /// 
    /// STRATEJİ: /Api/APIMethods/HealthCheck endpoint'ine GET isteği gönderir.
    /// 5sn timeout — başarısız veya yavaş yanıt raporlanır.
    /// </summary>
    public class MikroApiHealthCheck : IHealthCheck
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly MikroSettings _settings;
        private readonly ILogger<MikroApiHealthCheck> _logger;
        private static readonly TimeSpan CheckTimeout = TimeSpan.FromSeconds(5);

        public MikroApiHealthCheck(
            IHttpClientFactory httpClientFactory,
            IOptions<MikroSettings> settings,
            ILogger<MikroApiHealthCheck> logger)
        {
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(_settings.ApiUrl))
            {
                return HealthCheckResult.Degraded(
                    "Mikro API URL'si yapılandırılmamış.",
                    data: new Dictionary<string, object>
                    {
                        ["configured"] = false
                    });
            }

            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(CheckTimeout);

                var client = _httpClientFactory.CreateClient();
                client.Timeout = CheckTimeout;

                var healthUrl = $"{_settings.ApiUrl.TrimEnd('/')}/Api/APIMethods/HealthCheck";
                var response = await client.GetAsync(healthUrl, cts.Token);

                if (response.IsSuccessStatusCode)
                {
                    return HealthCheckResult.Healthy(
                        "Mikro HTTP API erişilebilir.",
                        data: new Dictionary<string, object>
                        {
                            ["statusCode"] = (int)response.StatusCode,
                            ["apiUrl"] = _settings.ApiUrl
                        });
                }

                return HealthCheckResult.Degraded(
                    $"Mikro HTTP API yanıt verdi ama HTTP {(int)response.StatusCode}.",
                    data: new Dictionary<string, object>
                    {
                        ["statusCode"] = (int)response.StatusCode
                    });
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("[HealthCheck] Mikro API bağlantı kontrolü zaman aşımına uğradı.");
                return HealthCheckResult.Degraded("Mikro API zaman aşımına uğradı (5sn).");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[HealthCheck] Mikro API bağlantı kontrolü başarısız.");
                return HealthCheckResult.Unhealthy(
                    "Mikro HTTP API erişilemiyor.",
                    exception: ex,
                    data: new Dictionary<string, object>
                    {
                        ["error"] = ex.Message,
                        ["apiUrl"] = _settings.ApiUrl ?? ""
                    });
            }
        }
    }
}
