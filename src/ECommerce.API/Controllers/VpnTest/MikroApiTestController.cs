using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using ECommerce.Infrastructure.Services.MicroServices;
using ECommerce.Infrastructure.Config;
using System;
using System.Threading.Tasks;

namespace ECommerce.API.Controllers.VpnTest
{
    /// <summary>
    /// VPN Test Mikro API Controller
    /// 
    /// Bu controller, VPN üzerinden erişilen Mikro API rotasını
    /// test etmek için kullanılabilir.
    /// 
    /// Kullanım:
    /// 1. ASPNETCORE_ENVIRONMENT = "VpnTest" ayarını yapın
    /// 2. dotnet run komutuyla başlatın
    /// 3. Aşağıdaki endpoint'leri test edin
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class MikroApiTestController : ControllerBase
    {
        private readonly MikroApiVpnTestService _mikroService;
        private readonly IOptions<MikroSettings> _mikroSettings;
        private readonly ILogger<MikroApiTestController> _logger;

        public MikroApiTestController(
            MikroApiVpnTestService mikroService,
            IOptions<MikroSettings> mikroSettings,
            ILogger<MikroApiTestController> logger)
        {
            _mikroService = mikroService;
            _mikroSettings = mikroSettings;
            _logger = logger;
        }

        /// <summary>
        /// GET: /api/mikroapitest/config
        /// Aktif Mikro API konfigürasyonunu göster (DEBUG amaçlı)
        /// </summary>
        [HttpGet("config")]
        [ProducesResponseType(typeof(ConfigResponse), StatusCodes.Status200OK)]
        public IActionResult GetConfig()
        {
            try
            {
                var config = _mikroSettings.Value;
                var isVpnRoute = UsesVpnRoute(config.ApiUrl);
                
                var response = new ConfigResponse
                {
                    ApiUrl = config.ApiUrl,
                    FirmaKodu = config.FirmaKodu,
                    KullaniciKodu = config.KullaniciKodu,
                    CalismaYili = config.CalismaYili,
                    DefaultDepoNo = config.DefaultDepoNo,
                    RequestTimeoutSeconds = config.RequestTimeoutSeconds,
                    IsVpnTest = isVpnRoute,
                    Message = "✅ Konfigürasyon başarıyla yüklendi"
                };

                _logger.LogInformation($"ℹ️ Config gösteriliyor: {config.ApiUrl}");
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Config hatası: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// POST: /api/mikroapitest/login
        /// Mikro API'ye giriş yap (Authentication test)
        /// </summary>
        [HttpPost("login")]
        [ProducesResponseType(typeof(ApiLoginResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Login()
        {
            try
            {
                _logger.LogInformation("🔐 Mikro API login isteği başlatıldı");
                var result = await _mikroService.LoginAsync();

                if (result.IsError)
                {
                    _logger.LogWarning($"⚠️ Login hatası: {result.ErrorMessage}");
                    return BadRequest(result);
                }

                _logger.LogInformation("✅ Login başarılı");
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Login exception: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// GET: /api/mikroapitest/product/{productKey}
        /// Ürün bilgisini sorgula
        /// 
        /// Örnek: /api/mikroapitest/product/URUN123
        /// </summary>
        [HttpGet("product/{productKey}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetProduct(string productKey)
        {
            if (string.IsNullOrEmpty(productKey))
            {
                return BadRequest(new { error = "Ürün anahtarı gerekli" });
            }

            try
            {
                _logger.LogInformation($"🔍 Ürün sorgulanıyor: {productKey}");
                var result = await _mikroService.GetProductAsync(productKey);

                return Ok(new
                {
                    success = true,
                    productKey = productKey,
                    data = result,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Ürün sorgusu hatası: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// GET: /api/mikroapitest/customer/{customerKey}
        /// Müşteri bilgisini sorgula
        /// 
        /// Örnek: /api/mikroapitest/customer/WEB-001
        /// </summary>
        [HttpGet("customer/{customerKey}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetCustomer(string customerKey)
        {
            if (string.IsNullOrEmpty(customerKey))
            {
                return BadRequest(new { error = "Müşteri anahtarı gerekli" });
            }

            try
            {
                _logger.LogInformation($"👥 Müşteri sorgulanıyor: {customerKey}");
                var result = await _mikroService.GetCustomerAsync(customerKey);

                return Ok(new
                {
                    success = true,
                    customerKey = customerKey,
                    data = result,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Müşteri sorgusu hatası: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// GET: /api/mikroapitest/system-info
        /// Mikro API sistem bilgisini al
        /// </summary>
        [HttpGet("system-info")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetSystemInfo()
        {
            try
            {
                _logger.LogInformation("⚙️ Sistem bilgisi alınıyor");
                var result = await _mikroService.GetSystemInfoAsync();

                return Ok(new
                {
                    success = true,
                    data = result,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Sistem bilgisi alma hatası: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// GET: /api/mikroapitest/health-check
        /// Mikro API'ye basit bir ping test
        /// </summary>
        [HttpGet("health-check")]
        [ProducesResponseType(typeof(HealthCheckResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(HealthCheckResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> HealthCheck()
        {
            var config = _mikroSettings.Value;
            var response = new HealthCheckResponse
            {
                Timestamp = DateTime.UtcNow,
                ApiUrl = config.ApiUrl,
                IsVpnTest = UsesVpnRoute(config.ApiUrl)
            };

            try
            {
                _logger.LogInformation($"❤️ Health check: {config.ApiUrl}");
                
                // Sistem bilgisi ile bağlantı test et
                var systemInfo = await _mikroService.GetSystemInfoAsync();
                
                response.Status = "Healthy ✅";
                response.Message = "Mikro API'ye başarıyla bağlanıldı";
                response.IsConnected = true;

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"⚠️ Health check başarısız: {ex.Message}");
                
                response.Status = "Unhealthy ❌";
                response.Message = $"Bağlantı hatası: {ex.Message}";
                response.IsConnected = false;

                return StatusCode(500, response);
            }
        }

        private static bool UsesVpnRoute(string? apiUrl)
        {
            if (string.IsNullOrWhiteSpace(apiUrl) ||
                !Uri.TryCreate(apiUrl, UriKind.Absolute, out var uri))
            {
                return false;
            }

            var host = uri.Host.ToLowerInvariant();
            return host == "10.0.0.3"
                || host == "mikro-vpn"
                || host.Contains("vpn");
        }
    }

    /// <summary>
    /// Konfigürasyon gösterimi için response modeli
    /// </summary>
    public class ConfigResponse
    {
        public string ApiUrl { get; set; }
        public string FirmaKodu { get; set; }
        public string KullaniciKodu { get; set; }
        public string CalismaYili { get; set; }
        public int DefaultDepoNo { get; set; }
        public int RequestTimeoutSeconds { get; set; }
        public bool IsVpnTest { get; set; }
        public string Message { get; set; }
    }

    /// <summary>
    /// Health check response modeli
    /// </summary>
    public class HealthCheckResponse
    {
        public DateTime Timestamp { get; set; }
        public string Status { get; set; }
        public string Message { get; set; }
        public string ApiUrl { get; set; }
        public bool IsVpnTest { get; set; }
        public bool IsConnected { get; set; }
    }
}
