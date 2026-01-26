using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using ECommerce.Core.DTOs.Weight;
using ECommerce.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ECommerce.API.Controllers
{
    /// <summary>
    /// Mikro API (tartı cihazı) webhook endpoint'i.
    /// 
    /// Güvenlik Özellikleri:
    /// - HMAC-SHA256 imza doğrulama
    /// - Timestamp kontrolü (replay attack önleme)
    /// - Rate limiting (ana middleware'den)
    /// - Idempotency (ExternalReportId ile)
    /// 
    /// Kullanım:
    /// POST /api/weight-reports/webhook
    /// Headers:
    ///   X-Webhook-Signature: {HMAC-SHA256 imzası}
    ///   X-Webhook-Timestamp: {Unix timestamp}
    /// Body: MicroWeightWebhookRequestDto JSON
    /// </summary>
    [ApiController]
    [Route("api/weight-reports")]
    [AllowAnonymous] // Webhook endpoint'i - kendi güvenlik mekanizması var
    public class WeightReportWebhookController : ControllerBase
    {
        private readonly IWeightService _weightService;
        private readonly ILogger<WeightReportWebhookController> _logger;
        private readonly IConfiguration _configuration;
        
        // Webhook imza için kullanılacak secret key
        private readonly string _webhookSecret;
        
        // Timestamp toleransı (saniye cinsinden)
        private const int TimestampToleranceSeconds = 300; // 5 dakika

        public WeightReportWebhookController(
            IWeightService weightService,
            ILogger<WeightReportWebhookController> logger,
            IConfiguration configuration)
        {
            _weightService = weightService ?? throw new ArgumentNullException(nameof(weightService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            
            // Secret key'i configuration'dan al
            _webhookSecret = _configuration["Webhook:WeightReport:Secret"] 
                ?? _configuration["WeightWebhookSecret"] 
                ?? "default-weight-webhook-secret-change-in-production";
        }

        /// <summary>
        /// Mikro API'den gelen tartı webhook'unu işler.
        /// 
        /// Akış:
        /// 1. İmza doğrulama (HMAC-SHA256)
        /// 2. Timestamp kontrolü
        /// 3. Payload validasyonu
        /// 4. WeightService.ProcessWebhookAsync çağrısı
        /// 5. Idempotent yanıt
        /// </summary>
        [HttpPost("webhook")]
        [Consumes("application/json")]
        [ProducesResponseType(typeof(MicroWeightWebhookResponseDto), 200)]
        [ProducesResponseType(typeof(object), 400)]
        [ProducesResponseType(typeof(object), 401)]
        public async Task<IActionResult> ReceiveWebhook([FromBody] MicroWeightWebhookRequestDto dto)
        {
            var requestId = Guid.NewGuid().ToString("N")[..8];
            
            try
            {
                _logger.LogInformation("[{RequestId}] Tartı webhook alındı: OrderId={OrderId}, ReportId={ReportId}",
                    requestId, dto.OrderId, dto.ReportId);

                // 1. İmza doğrulama
                var signatureValidation = await ValidateWebhookSignatureAsync();
                if (!signatureValidation.IsValid)
                {
                    _logger.LogWarning("[{RequestId}] Webhook imza doğrulama başarısız: {Reason}",
                        requestId, signatureValidation.ErrorMessage);
                    
                    return Unauthorized(new 
                    { 
                        error = "Invalid signature",
                        message = signatureValidation.ErrorMessage,
                        requestId
                    });
                }

                // 2. Timestamp kontrolü
                if (!ValidateTimestamp(out var timestampError))
                {
                    _logger.LogWarning("[{RequestId}] Webhook timestamp geçersiz: {Reason}",
                        requestId, timestampError);
                    
                    return BadRequest(new 
                    { 
                        error = "Invalid timestamp",
                        message = timestampError,
                        requestId
                    });
                }

                // 3. Model validasyonu (attribute'lardan)
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("[{RequestId}] Webhook payload validasyon hatası",
                        requestId);
                    
                    return BadRequest(new 
                    { 
                        error = "Validation error",
                        details = ModelState,
                        requestId
                    });
                }

                // 4. WeightService'e ilet
                var result = await _weightService.ProcessWebhookAsync(dto);

                // 5. Yanıt
                if (result.Success)
                {
                    _logger.LogInformation("[{RequestId}] Tartı webhook başarıyla işlendi: ReportId={ReportId}, Status={Status}",
                        requestId, result.ReportId, result.Status);
                    
                    return Ok(result);
                }
                else
                {
                    _logger.LogWarning("[{RequestId}] Tartı webhook işleme hatası: {ErrorCode} - {Message}",
                        requestId, result.ErrorCode, result.Message);
                    
                    // Idempotency: Duplicate durumda da 200 dön
                    if (result.ErrorCode == "DUPLICATE")
                    {
                        return Ok(result);
                    }
                    
                    return BadRequest(result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[{RequestId}] Tartı webhook işlenirken beklenmeyen hata",
                    requestId);
                
                return StatusCode(500, new 
                { 
                    error = "Internal server error",
                    message = "Webhook işlenirken bir hata oluştu",
                    requestId
                });
            }
        }

        /// <summary>
        /// Kurye tarafından manuel tartı farkı girişi.
        /// Mikro API olmadığında kullanılır.
        /// </summary>
        [HttpPost("courier-adjustment")]
        [Authorize(Roles = "Courier")]
        [ProducesResponseType(typeof(CourierWeightAdjustmentResponseDto), 200)]
        [ProducesResponseType(typeof(object), 400)]
        [ProducesResponseType(typeof(object), 401)]
        public async Task<IActionResult> CourierAdjustment([FromBody] CourierWeightAdjustmentDto dto)
        {
            try
            {
                // Kurye ID'sini JWT'den al
                var courierIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(courierIdClaim) || !int.TryParse(courierIdClaim, out var userId))
                {
                    return Unauthorized(new { error = "Geçersiz kullanıcı" });
                }

                // CourierId'yi bul (User → Courier ilişkisi)
                // Not: Bu kısım CourierOrderService'deki GetCourierIdByUserIdAsync ile benzer
                // Gerçek implementasyonda servis kullanılmalı
                
                _logger.LogInformation("Kurye (UserId={UserId}) tartı farkı giriyor: OrderId={OrderId}, Fark={Diff}g",
                    userId, dto.OrderId, dto.WeightDifferenceGrams);

                // WeightService'e ilet
                var result = await _weightService.ProcessCourierAdjustmentAsync(dto, userId);

                if (result.Success)
                {
                    return Ok(result);
                }
                else
                {
                    return BadRequest(result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kurye tartı farkı işlenirken hata");
                return StatusCode(500, new { error = "İşlem sırasında hata oluştu" });
            }
        }

        /// <summary>
        /// Belirli bir sipariş için tartı raporlarını getirir.
        /// </summary>
        [HttpGet("order/{orderId}")]
        [Authorize(Roles = "Admin,Courier")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetReportsForOrder(int orderId)
        {
            try
            {
                var reports = await _weightService.GetPendingReportsForOrderAsync(orderId);
                var totalDifferenceAmount = await _weightService.GetTotalWeightDifferenceAmountAsync(orderId);
                var finalAmount = await _weightService.CalculateFinalAmountForOrderAsync(orderId);

                return Ok(new
                {
                    orderId,
                    reports = reports,
                    summary = new
                    {
                        totalDifferenceAmount,
                        finalAmount,
                        pendingReportsCount = reports.Count()
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sipariş #{OrderId} tartı raporları alınırken hata", orderId);
                return StatusCode(500, new { error = "Raporlar alınamadı" });
            }
        }

        #region Webhook Güvenlik Doğrulama

        /// <summary>
        /// Webhook imzasını doğrular (HMAC-SHA256).
        /// 
        /// İmza hesaplama:
        /// HMAC-SHA256(timestamp + "." + requestBody, secret)
        /// </summary>
        private async Task<(bool IsValid, string? ErrorMessage)> ValidateWebhookSignatureAsync()
        {
            // Development ortamında imza doğrulamayı atla
            var environment = _configuration["ASPNETCORE_ENVIRONMENT"];
            if (environment == "Development")
            {
                _logger.LogDebug("Development ortamında imza doğrulama atlandı");
                return (true, null);
            }

            // X-Webhook-Signature header'ını al
            if (!Request.Headers.TryGetValue("X-Webhook-Signature", out var signatureHeader))
            {
                return (false, "X-Webhook-Signature header eksik");
            }

            // X-Webhook-Timestamp header'ını al
            if (!Request.Headers.TryGetValue("X-Webhook-Timestamp", out var timestampHeader))
            {
                return (false, "X-Webhook-Timestamp header eksik");
            }

            var providedSignature = signatureHeader.ToString();
            var timestamp = timestampHeader.ToString();

            // Request body'yi oku
            Request.Body.Position = 0;
            using var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true);
            var requestBody = await reader.ReadToEndAsync();
            Request.Body.Position = 0;

            // Beklenen imzayı hesapla
            var payload = $"{timestamp}.{requestBody}";
            var expectedSignature = ComputeHmacSha256(payload, _webhookSecret);

            // İmzaları karşılaştır (timing-safe)
            if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(providedSignature),
                Encoding.UTF8.GetBytes(expectedSignature)))
            {
                return (false, "İmza doğrulaması başarısız");
            }

            return (true, null);
        }

        /// <summary>
        /// Timestamp'ın geçerli aralıkta olup olmadığını kontrol eder.
        /// Replay attack'ları önler.
        /// </summary>
        private bool ValidateTimestamp(out string? errorMessage)
        {
            errorMessage = null;

            // Development ortamında timestamp doğrulamayı atla
            var environment = _configuration["ASPNETCORE_ENVIRONMENT"];
            if (environment == "Development")
            {
                return true;
            }

            if (!Request.Headers.TryGetValue("X-Webhook-Timestamp", out var timestampHeader))
            {
                errorMessage = "X-Webhook-Timestamp header eksik";
                return false;
            }

            if (!long.TryParse(timestampHeader.ToString(), out var unixTimestamp))
            {
                errorMessage = "Geçersiz timestamp formatı";
                return false;
            }

            var webhookTime = DateTimeOffset.FromUnixTimeSeconds(unixTimestamp);
            var now = DateTimeOffset.UtcNow;
            var difference = Math.Abs((now - webhookTime).TotalSeconds);

            if (difference > TimestampToleranceSeconds)
            {
                errorMessage = $"Timestamp çok eski veya gelecekte ({difference:F0} saniye fark)";
                return false;
            }

            return true;
        }

        /// <summary>
        /// HMAC-SHA256 hesaplar.
        /// </summary>
        private static string ComputeHmacSha256(string data, string secret)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        #endregion
    }
}
