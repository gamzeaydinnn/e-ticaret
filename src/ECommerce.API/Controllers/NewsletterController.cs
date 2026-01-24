using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using ECommerce.Business.Services.Interfaces;
using ECommerce.API.DTOs.Newsletter;

namespace ECommerce.API.Controllers
{
    /// <summary>
    /// Newsletter (Bülten) Public API Controller.
    /// 
    /// AMAÇ:
    /// - Kullanıcıların bültene abone olmasını sağlar
    /// - GDPR uyumlu token bazlı abonelik iptali
    /// - Authentication gerektirmez (public endpoint'ler)
    /// 
    /// GÜVENLİK:
    /// - Rate limiting ile spam koruması
    /// - Input validation ve sanitization
    /// - IP adresi kaydı (KVKK kanıtı)
    /// 
    /// ENDPOINT'LER:
    /// - POST /api/newsletter/subscribe - Bültene abone ol
    /// - GET  /api/newsletter/unsubscribe?token=xxx - Abonelikten çık (email linki)
    /// - POST /api/newsletter/unsubscribe - Abonelikten çık (form)
    /// </summary>
    [ApiController]
    [Route("api/newsletter")]
    public class NewsletterController : ControllerBase
    {
        private readonly INewsletterService _newsletterService;
        private readonly ILogger<NewsletterController> _logger;
        private readonly IConfiguration _configuration;

        public NewsletterController(
            INewsletterService newsletterService,
            ILogger<NewsletterController> logger,
            IConfiguration configuration)
        {
            _newsletterService = newsletterService ?? throw new ArgumentNullException(nameof(newsletterService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        // ═══════════════════════════════════════════════════════════════════════════════
        // ABONELİK ENDPOINT'LERİ
        // Public - Authentication gerektirmez
        // ═══════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Bültene abone olma endpoint'i.
        /// 
        /// ÇALIŞMA PRENSİBİ:
        /// 1. Email validasyonu yapılır
        /// 2. IP adresi alınır (KVKK kanıtı)
        /// 3. Eğer email mevcutsa ve aktifse: "zaten abone" döner
        /// 4. Eğer email mevcutsa ve pasifse: resubscribe yapılır
        /// 5. Eğer yeni email ise: yeni kayıt oluşturulur
        /// 
        /// GÜVENLİK:
        /// - Email format validasyonu (DTO attribute)
        /// - HTML/Script sanitization (service layer)
        /// - IP adresi kaydı (X-Forwarded-For header desteği)
        /// </summary>
        /// <param name="request">Abonelik isteği (email, fullName, source)</param>
        /// <returns>Abonelik sonucu</returns>
        [HttpPost("subscribe")]
        [ProducesResponseType(typeof(NewsletterSubscribeResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(NewsletterSubscribeResponseDto), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Subscribe([FromBody] NewsletterSubscribeRequestDto request)
        {
            try
            {
                // ══════════════════════════════════════════════════════════════════════
                // VALİDASYON
                // ModelState otomatik olarak DTO attribute'larını kontrol eder
                // ══════════════════════════════════════════════════════════════════════

                if (!ModelState.IsValid)
                {
                    return BadRequest(new NewsletterSubscribeResponseDto
                    {
                        Success = false,
                        Message = "Geçersiz veri. Lütfen e-posta adresinizi kontrol edin."
                    });
                }

                // ══════════════════════════════════════════════════════════════════════
                // IP ADRESİ ALMA
                // Proxy/Load balancer arkasında X-Forwarded-For header'ı kullanılır
                // KVKK kanıtı için abonelik onayının IP'si saklanır
                // ══════════════════════════════════════════════════════════════════════

                var ipAddress = GetClientIpAddress();

                // ══════════════════════════════════════════════════════════════════════
                // ABONELİK İŞLEMİ
                // Service layer'da email normalizasyonu ve business logic uygulanır
                // ══════════════════════════════════════════════════════════════════════

                // Kaynak belirleme - frontend'den gelmezse varsayılan
                var source = string.IsNullOrWhiteSpace(request.Source) ? "web_footer" : request.Source;

                // Giriş yapmış kullanıcı varsa ID'sini al
                int? userId = null;
                if (User.Identity?.IsAuthenticated == true)
                {
                    var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                    if (int.TryParse(userIdClaim, out int parsedUserId))
                    {
                        userId = parsedUserId;
                    }
                }

                var result = await _newsletterService.SubscribeAsync(
                    request.Email,
                    request.FullName,
                    source,
                    ipAddress,
                    userId);

                // Başarılı/başarısız duruma göre HTTP status kodu
                if (result.Success)
                {
                    _logger.LogInformation(
                        "Newsletter subscription: Email={Email}, Source={Source}, IP={IP}",
                        request.Email, source, ipAddress);

                    return Ok(new NewsletterSubscribeResponseDto
                    {
                        Success = true,
                        Message = result.Message,
                        SubscriberId = result.SubscriberId,
                        WasAlreadySubscribed = result.WasAlreadySubscribed
                    });
                }

                return BadRequest(new NewsletterSubscribeResponseDto
                {
                    Success = false,
                    Message = result.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during newsletter subscription: {Email}", request?.Email);

                return StatusCode(StatusCodes.Status500InternalServerError, new NewsletterSubscribeResponseDto
                {
                    Success = false,
                    Message = "Abonelik işlemi sırasında bir hata oluştu. Lütfen daha sonra tekrar deneyin."
                });
            }
        }

        /// <summary>
        /// Token bazlı abonelik iptal endpoint'i (GET).
        /// Email içindeki "Abonelikten Çık" linki bu endpoint'e yönlendirir.
        /// 
        /// GDPR UYUMU:
        /// - Login gerektirmez
        /// - Tek tıkla abonelik iptali
        /// - Token benzersiz ve tahmin edilemez (GUID)
        /// 
        /// KULLANIM:
        /// GET /api/newsletter/unsubscribe?token=abc123...
        /// 
        /// Frontend'de bir "başarılı iptal" sayfasına yönlendirilebilir.
        /// </summary>
        /// <param name="token">Benzersiz abonelik iptal token'ı</param>
        /// <returns>İptal sonucu veya yönlendirme</returns>
        [HttpGet("unsubscribe")]
        [ProducesResponseType(typeof(NewsletterUnsubscribeResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(NewsletterUnsubscribeResponseDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(NewsletterUnsubscribeResponseDto), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UnsubscribeByToken([FromQuery] string token)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(token))
                {
                    return BadRequest(new NewsletterUnsubscribeResponseDto
                    {
                        Success = false,
                        Message = "Geçersiz abonelik iptal linki."
                    });
                }

                var result = await _newsletterService.UnsubscribeByTokenAsync(token);

                if (result.Success)
                {
                    _logger.LogInformation("Newsletter unsubscribe by token successful: {Token}", token[..Math.Min(8, token.Length)] + "...");

                    // Frontend'e yönlendirme yapılabilir
                    // return Redirect("/unsubscribe-success");

                    return Ok(new NewsletterUnsubscribeResponseDto
                    {
                        Success = true,
                        Message = result.Message
                    });
                }

                // Token bulunamadı veya geçersiz
                return NotFound(new NewsletterUnsubscribeResponseDto
                {
                    Success = false,
                    Message = result.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during newsletter unsubscribe by token");

                return StatusCode(StatusCodes.Status500InternalServerError, new NewsletterUnsubscribeResponseDto
                {
                    Success = false,
                    Message = "Abonelik iptal işlemi sırasında bir hata oluştu."
                });
            }
        }

        /// <summary>
        /// Form bazlı abonelik iptal endpoint'i (POST).
        /// Token veya sebep ile birlikte iptal isteği gönderilebilir.
        /// </summary>
        /// <param name="request">İptal isteği (token, reason)</param>
        /// <returns>İptal sonucu</returns>
        [HttpPost("unsubscribe")]
        [ProducesResponseType(typeof(NewsletterUnsubscribeResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(NewsletterUnsubscribeResponseDto), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UnsubscribeByForm([FromBody] NewsletterUnsubscribeRequestDto request)
        {
            try
            {
                if (!ModelState.IsValid || string.IsNullOrWhiteSpace(request.Token))
                {
                    return BadRequest(new NewsletterUnsubscribeResponseDto
                    {
                        Success = false,
                        Message = "Geçersiz abonelik iptal isteği."
                    });
                }

                var result = await _newsletterService.UnsubscribeByTokenAsync(request.Token, request.Reason);

                if (result.Success)
                {
                    _logger.LogInformation(
                        "Newsletter unsubscribe by form: Reason={Reason}",
                        request.Reason ?? "not specified");

                    return Ok(new NewsletterUnsubscribeResponseDto
                    {
                        Success = true,
                        Message = result.Message
                    });
                }

                return BadRequest(new NewsletterUnsubscribeResponseDto
                {
                    Success = false,
                    Message = result.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during newsletter unsubscribe by form");

                return StatusCode(StatusCodes.Status500InternalServerError, new NewsletterUnsubscribeResponseDto
                {
                    Success = false,
                    Message = "Abonelik iptal işlemi sırasında bir hata oluştu."
                });
            }
        }

        // ═══════════════════════════════════════════════════════════════════════════════
        // YARDIMCI METODLAR
        // ═══════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// İstemci IP adresini alır.
        /// Proxy/Load balancer arkasında X-Forwarded-For header'ı kullanılır.
        /// </summary>
        private string GetClientIpAddress()
        {
            // X-Forwarded-For header'ı varsa (proxy arkasında)
            var forwardedFor = Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(forwardedFor))
            {
                // İlk IP adresi gerçek istemci IP'sidir
                var ips = forwardedFor.Split(',', StringSplitOptions.RemoveEmptyEntries);
                if (ips.Length > 0)
                {
                    return ips[0].Trim();
                }
            }

            // X-Real-IP header'ı (Nginx)
            var realIp = Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(realIp))
            {
                return realIp.Trim();
            }

            // Doğrudan bağlantı IP'si
            return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }
    }
}
