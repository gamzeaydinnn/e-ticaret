using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ECommerce.API.DTOs.Sms;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Entities.Enums;

namespace ECommerce.API.Controllers
{
    /// <summary>
    /// SMS Doğrulama Controller.
    /// 
    /// Bu controller SMS tabanlı OTP (One-Time Password) doğrulama işlemlerini yönetir.
    /// Kayıt, şifre sıfırlama ve iki faktörlü kimlik doğrulama için kullanılır.
    /// 
    /// Güvenlik Özellikleri:
    /// - Rate limiting (IP ve telefon numarası bazlı)
    /// - 60 saniye resend cooldown
    /// - Günde maksimum 5 OTP/numara
    /// - Maksimum 3 yanlış deneme
    /// - 3 dakika kod geçerliliği
    /// 
    /// SOLID: Single Responsibility - Sadece SMS doğrulama API endpoint'leri
    /// </summary>
    [ApiController]
    [Route("api/sms")]
    [AllowAnonymous] // SMS doğrulama genelde login gerektirmez
    [Produces("application/json")]
    public class SmsVerificationController : ControllerBase
    {
        private readonly ISmsVerificationService _smsVerificationService;
        private readonly ILogger<SmsVerificationController> _logger;

        public SmsVerificationController(
            ISmsVerificationService smsVerificationService,
            ILogger<SmsVerificationController> logger)
        {
            _smsVerificationService = smsVerificationService 
                ?? throw new ArgumentNullException(nameof(smsVerificationService));
            _logger = logger 
                ?? throw new ArgumentNullException(nameof(logger));
        }

        #region Endpoint'ler

        /// <summary>
        /// OTP kodu gönderir.
        /// 
        /// Belirtilen telefon numarasına 6 haneli doğrulama kodu gönderir.
        /// Rate limiting uygulanır: 60 saniye cooldown, günde 5 SMS limiti.
        /// </summary>
        /// <param name="request">Telefon numarası ve doğrulama amacı</param>
        /// <returns>Gönderim sonucu</returns>
        /// <response code="200">OTP başarıyla gönderildi</response>
        /// <response code="400">Geçersiz telefon numarası</response>
        /// <response code="429">Çok fazla istek (rate limit)</response>
        /// <response code="500">Sunucu hatası</response>
        [HttpPost("send-otp")]
        [ProducesResponseType(typeof(SmsVerificationResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(SmsVerificationResponseDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(SmsVerificationResponseDto), StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(SmsVerificationResponseDto), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SendOtp([FromBody] SendOtpRequestDto request)
        {
            try
            {
                // Model validation FluentValidation tarafından yapılır
                if (!ModelState.IsValid)
                {
                    return BadRequest(SmsVerificationResponseDto.Error(
                        "Geçersiz istek parametreleri.",
                        "VALIDATION_ERROR"));
                }

                // IP adresi ve User-Agent bilgisi al
                var ipAddress = GetClientIpAddress();
                var userAgent = GetUserAgent();
                var userId = GetCurrentUserId();

                _logger.LogInformation(
                    "[SMS API] OTP gönderim isteği - Phone: {Phone}, Purpose: {Purpose}, IP: {IP}",
                    MaskPhoneNumber(request.PhoneNumber),
                    request.Purpose,
                    ipAddress);

                // SMS doğrulama servisini çağır
                var result = await _smsVerificationService.SendVerificationCodeAsync(
                    request.PhoneNumber,
                    request.Purpose,
                    ipAddress,
                    userAgent,
                    userId);

                // Sonucu HTTP yanıtına dönüştür
                return MapResultToResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SMS API] OTP gönderim hatası - Phone: {Phone}", 
                    MaskPhoneNumber(request.PhoneNumber));
                
                return StatusCode(StatusCodes.Status500InternalServerError,
                    SmsVerificationResponseDto.Error(
                        "Beklenmeyen bir hata oluştu. Lütfen daha sonra tekrar deneyin.",
                        "INTERNAL_ERROR"));
            }
        }

        /// <summary>
        /// OTP kodunu doğrular.
        /// 
        /// Kullanıcının girdiği 6 haneli kodu doğrular.
        /// Maksimum 3 yanlış deneme hakkı vardır.
        /// </summary>
        /// <param name="request">Telefon numarası, kod ve doğrulama amacı</param>
        /// <returns>Doğrulama sonucu</returns>
        /// <response code="200">Doğrulama başarılı</response>
        /// <response code="400">Yanlış kod veya geçersiz istek</response>
        /// <response code="500">Sunucu hatası</response>
        [HttpPost("verify-otp")]
        [ProducesResponseType(typeof(SmsVerificationResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(SmsVerificationResponseDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(SmsVerificationResponseDto), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequestDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(SmsVerificationResponseDto.Error(
                        "Geçersiz istek parametreleri.",
                        "VALIDATION_ERROR"));
                }

                var ipAddress = GetClientIpAddress();

                _logger.LogInformation(
                    "[SMS API] OTP doğrulama isteği - Phone: {Phone}, Purpose: {Purpose}",
                    MaskPhoneNumber(request.PhoneNumber),
                    request.Purpose);

                var result = await _smsVerificationService.VerifyCodeAsync(
                    request.PhoneNumber,
                    request.Code,
                    request.Purpose,
                    ipAddress);

                return MapResultToResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SMS API] OTP doğrulama hatası - Phone: {Phone}",
                    MaskPhoneNumber(request.PhoneNumber));

                return StatusCode(StatusCodes.Status500InternalServerError,
                    SmsVerificationResponseDto.Error(
                        "Beklenmeyen bir hata oluştu.",
                        "INTERNAL_ERROR"));
            }
        }

        /// <summary>
        /// OTP kodunu tekrar gönderir.
        /// 
        /// Önceki kodu iptal edip yeni bir kod gönderir.
        /// 60 saniye cooldown süresi uygulanır.
        /// </summary>
        /// <param name="request">Telefon numarası ve doğrulama amacı</param>
        /// <returns>Gönderim sonucu</returns>
        /// <response code="200">OTP başarıyla tekrar gönderildi</response>
        /// <response code="429">Çok erken tekrar gönderim (cooldown)</response>
        [HttpPost("resend-otp")]
        [ProducesResponseType(typeof(SmsVerificationResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(SmsVerificationResponseDto), StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> ResendOtp([FromBody] ResendOtpRequestDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(SmsVerificationResponseDto.Error(
                        "Geçersiz istek parametreleri.",
                        "VALIDATION_ERROR"));
                }

                var ipAddress = GetClientIpAddress();
                var userAgent = GetUserAgent();
                var userId = GetCurrentUserId();

                _logger.LogInformation(
                    "[SMS API] OTP tekrar gönderim isteği - Phone: {Phone}",
                    MaskPhoneNumber(request.PhoneNumber));

                // Resend = yeni kod gönder (servis önceki pending kayıtları iptal eder)
                var result = await _smsVerificationService.SendVerificationCodeAsync(
                    request.PhoneNumber,
                    request.Purpose,
                    ipAddress,
                    userAgent,
                    userId);

                return MapResultToResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SMS API] OTP tekrar gönderim hatası");

                return StatusCode(StatusCodes.Status500InternalServerError,
                    SmsVerificationResponseDto.Error(
                        "Beklenmeyen bir hata oluştu.",
                        "INTERNAL_ERROR"));
            }
        }

        /// <summary>
        /// Belirli bir telefon numarası için doğrulama durumunu sorgular.
        /// 
        /// Aktif doğrulama kaydı, kalan süre, kalan deneme hakkı ve
        /// resend durumu hakkında bilgi verir.
        /// </summary>
        /// <param name="phone">Telefon numarası (URL encoded)</param>
        /// <param name="purpose">Doğrulama amacı (opsiyonel, varsayılan: Registration)</param>
        /// <returns>Doğrulama durumu</returns>
        /// <response code="200">Durum bilgisi döndürüldü</response>
        /// <response code="400">Geçersiz telefon numarası</response>
        [HttpGet("status/{phone}")]
        [ProducesResponseType(typeof(SmsStatusResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(SmsVerificationResponseDto), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetStatus(
            [FromRoute] string phone,
            [FromQuery] SmsVerificationPurpose purpose = SmsVerificationPurpose.Registration)
        {
            try
            {
                // Basit telefon numarası validasyonu
                if (string.IsNullOrWhiteSpace(phone))
                {
                    return BadRequest(SmsVerificationResponseDto.Error(
                        "Telefon numarası gereklidir.",
                        "INVALID_PHONE"));
                }

                _logger.LogDebug("[SMS API] Durum sorgusu - Phone: {Phone}", MaskPhoneNumber(phone));

                // Rate limit bilgisi al
                var ipAddress = GetClientIpAddress();
                var rateLimitResult = await _smsVerificationService.CanSendVerificationAsync(phone, ipAddress);

                // Bloklu numara kontrolü
                if (rateLimitResult.IsBlocked)
                {
                    return Ok(SmsStatusResponseDto.Blocked(rateLimitResult.BlockedUntil!.Value));
                }

                // Doğrulama durumu al
                var statusResult = await _smsVerificationService.GetVerificationStatusAsync(phone, purpose);

                if (!statusResult.HasActiveVerification)
                {
                    return Ok(new SmsStatusResponseDto
                    {
                        HasActiveVerification = false,
                        Status = "None",
                        CanResend = rateLimitResult.CanSend,
                        RemainingDailyCount = rateLimitResult.RemainingDailyCount,
                        ResendAfterSeconds = rateLimitResult.CanSend ? 0 : rateLimitResult.RetryAfterSeconds
                    });
                }

                return Ok(new SmsStatusResponseDto
                {
                    HasActiveVerification = true,
                    Status = statusResult.Status,
                    RemainingSeconds = statusResult.RemainingSeconds,
                    RemainingAttempts = statusResult.RemainingAttempts,
                    ResendAfterSeconds = statusResult.ResendAfterSeconds,
                    CanResend = statusResult.ResendAfterSeconds <= 0 && rateLimitResult.RemainingDailyCount > 0,
                    RemainingDailyCount = rateLimitResult.RemainingDailyCount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SMS API] Durum sorgusu hatası");

                return StatusCode(StatusCodes.Status500InternalServerError,
                    SmsVerificationResponseDto.Error(
                        "Beklenmeyen bir hata oluştu.",
                        "INTERNAL_ERROR"));
            }
        }

        /// <summary>
        /// OTP gönderilebilir mi kontrolü (rate limit).
        /// Frontend'in buton durumunu belirlemesi için kullanılır.
        /// </summary>
        /// <param name="phone">Telefon numarası</param>
        /// <returns>Gönderilebilir mi?</returns>
        [HttpGet("can-send")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public async Task<IActionResult> CanSend([FromQuery] string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
            {
                return Ok(new { canSend = false, reason = "Telefon numarası gerekli" });
            }

            var ipAddress = GetClientIpAddress();
            var result = await _smsVerificationService.CanSendVerificationAsync(phone, ipAddress);

            return Ok(new
            {
                canSend = result.CanSend,
                reason = result.Reason,
                retryAfterSeconds = result.RetryAfterSeconds,
                remainingDailyCount = result.RemainingDailyCount,
                isBlocked = result.IsBlocked,
                blockedUntil = result.BlockedUntil
            });
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Servis sonucunu HTTP yanıtına dönüştürür.
        /// </summary>
        private IActionResult MapResultToResponse(SmsVerificationResult result)
        {
            var response = new SmsVerificationResponseDto
            {
                Success = result.Success,
                Message = result.Message,
                ErrorCode = result.ErrorCode,
                ExpiresInSeconds = result.ExpiresInSeconds,
                RemainingAttempts = result.RemainingAttempts,
                RetryAfterSeconds = result.RetryAfterSeconds,
                VerificationId = result.VerificationId
            };

            // Rate limit durumunda 429 döndür
            if (!result.Success && result.ErrorCode == "RATE_LIMITED")
            {
                // Retry-After header ekle (RFC 6585)
                if (result.RetryAfterSeconds.HasValue)
                {
                    Response.Headers["Retry-After"] = result.RetryAfterSeconds.Value.ToString();
                }
                return StatusCode(StatusCodes.Status429TooManyRequests, response);
            }

            // Bloklu numara durumunda 403 döndür
            if (!result.Success && result.ErrorCode == "PHONE_BLOCKED")
            {
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }

            // Başarısız durumlar için 400 döndür
            if (!result.Success)
            {
                return BadRequest(response);
            }

            // Başarılı durumda 200 döndür
            return Ok(response);
        }

        /// <summary>
        /// İstemci IP adresini alır.
        /// Proxy arkasındaki gerçek IP'yi tespit etmeye çalışır.
        /// </summary>
        private string? GetClientIpAddress()
        {
            // X-Forwarded-For header'ı kontrol et (proxy/load balancer arkasında)
            var forwardedFor = Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                // İlk IP gerçek client IP'dir
                return forwardedFor.Split(',')[0].Trim();
            }

            // X-Real-IP header'ı kontrol et (nginx)
            var realIp = Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrEmpty(realIp))
            {
                return realIp;
            }

            // Doğrudan bağlantı IP'si
            return HttpContext.Connection.RemoteIpAddress?.ToString();
        }

        /// <summary>
        /// User-Agent header'ını alır.
        /// </summary>
        private string? GetUserAgent()
        {
            return Request.Headers.UserAgent.FirstOrDefault();
        }

        /// <summary>
        /// Giriş yapmış kullanıcının ID'sini alır (varsa).
        /// </summary>
        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
            {
                return userId;
            }
            return null;
        }

        /// <summary>
        /// Telefon numarasını log için maskeler.
        /// Güvenlik için son 4 hane dışında gizler.
        /// Örnek: 5331234567 -> 533***4567
        /// </summary>
        private static string MaskPhoneNumber(string? phone)
        {
            if (string.IsNullOrWhiteSpace(phone) || phone.Length < 7)
                return "***";

            var digits = phone.Where(char.IsDigit).ToArray();
            if (digits.Length < 7)
                return "***";

            // İlk 3 ve son 4 rakamı göster
            return $"{new string(digits[..3])}***{new string(digits[^4..])}";
        }

        #endregion
    }
}
