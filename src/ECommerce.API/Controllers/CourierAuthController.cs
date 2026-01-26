using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Core.DTOs.Courier;

namespace ECommerce.API.Controllers
{
    /// <summary>
    /// Kurye Authentication Controller.
    /// 
    /// Endpoint'ler:
    /// - POST /api/courier/auth/login      - Kurye giriş (JWT + Refresh token döndürür)
    /// - POST /api/courier/auth/logout     - Kurye çıkış (Token geçersizleştirme)
    /// - POST /api/courier/auth/refresh    - Token yenileme
    /// - POST /api/courier/auth/change-password - Şifre değiştirme (kendi şifresini)
    /// - GET  /api/courier/auth/me         - Mevcut kurye bilgileri
    /// 
    /// Güvenlik:
    /// - Rate limiting (login endpoint için)
    /// - JWT authentication (logout, change-password, me için)
    /// - Role-based authorization (sadece Courier rolü)
    /// - Token deny list entegrasyonu
    /// </summary>
    [ApiController]
    [Route("api/courier/auth")]
    [Produces("application/json")]
    public class CourierAuthController : ControllerBase
    {
        private readonly ICourierAuthService _courierAuthService;
        private readonly ILoginRateLimitService _loginRateLimitService;
        private readonly ILogger<CourierAuthController> _logger;

        // Maksimum başarısız giriş denemesi
        private const int MAX_LOGIN_ATTEMPTS = 5;
        // Blokaj süresi (dakika)
        private const int LOCKOUT_MINUTES = 15;

        public CourierAuthController(
            ICourierAuthService courierAuthService,
            ILoginRateLimitService loginRateLimitService,
            ILogger<CourierAuthController> logger)
        {
            _courierAuthService = courierAuthService ?? throw new ArgumentNullException(nameof(courierAuthService));
            _loginRateLimitService = loginRateLimitService;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Kurye giriş endpoint'i.
        /// E-posta ve şifre ile giriş yapılır, JWT access token + refresh token döndürülür.
        /// </summary>
        /// <param name="dto">Giriş bilgileri (email, password, rememberMe)</param>
        /// <returns>JWT tokens ve kurye bilgileri</returns>
        /// <response code="200">Giriş başarılı</response>
        /// <response code="400">Geçersiz istek veya giriş bilgileri</response>
        /// <response code="429">Çok fazla başarısız deneme (rate limit)</response>
        [HttpPost("login")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(CourierLoginResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(object), StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> Login([FromBody] CourierLoginDto dto)
        {
            try
            {
                // 1. Input validation
                if (dto == null || string.IsNullOrWhiteSpace(dto.Email))
                {
                    return BadRequest(new { Success = false, Message = "E-posta adresi zorunludur." });
                }

                // 2. Rate limiting kontrolü
                if (_loginRateLimitService != null && _loginRateLimitService.IsBlocked(dto.Email, out var remaining))
                {
                    var minutes = Math.Ceiling(remaining.TotalMinutes);
                    _logger.LogWarning("Kurye giriş engellendi (rate limit): {Email}, Kalan: {Minutes} dakika", dto.Email, minutes);
                    
                    return StatusCode(StatusCodes.Status429TooManyRequests, new 
                    { 
                        Success = false, 
                        Message = $"Çok fazla başarısız deneme. Lütfen {minutes} dakika sonra tekrar deneyin.",
                        RetryAfterMinutes = minutes
                    });
                }

                // 3. İstemci IP adresini al
                var ipAddress = GetClientIpAddress();

                // 4. Giriş işlemi
                var result = await _courierAuthService.LoginAsync(dto, ipAddress);

                // 5. Başarılı giriş
                if (result.Success)
                {
                    // Rate limit sayacını sıfırla
                    try
                    {
                        _loginRateLimitService?.ResetAttempts(dto.Email);
                    }
                    catch
                    {
                        // Rate limit cache hatalarını yutuyoruz
                    }

                    _logger.LogInformation("Kurye giriş başarılı: {Email}, CourierId: {CourierId}", 
                        dto.Email, result.Courier?.CourierId);

                    return Ok(result);
                }

                // 6. Başarısız giriş - Rate limit sayacını artır
                try
                {
                    if (_loginRateLimitService != null)
                    {
                        var attempts = _loginRateLimitService.IncrementFailedAttempt(dto.Email);
                        if (attempts >= MAX_LOGIN_ATTEMPTS)
                        {
                            _logger.LogWarning("Kurye hesabı geçici olarak bloke edildi: {Email}, Deneme: {Attempts}", dto.Email, attempts);
                            return BadRequest(new 
                            { 
                                Success = false, 
                                Message = $"Çok fazla başarısız deneme. {LOCKOUT_MINUTES} dakika boyunca bloke edildiniz." 
                            });
                        }
                    }
                }
                catch
                {
                    // Rate limit cache hatalarını yutuyoruz
                }

                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kurye giriş endpoint hatası: {Email}", dto?.Email);
                return StatusCode(StatusCodes.Status500InternalServerError, new 
                { 
                    Success = false, 
                    Message = "Giriş sırasında bir hata oluştu. Lütfen tekrar deneyin." 
                });
            }
        }

        /// <summary>
        /// Kurye çıkış endpoint'i.
        /// Mevcut JWT token geçersiz kılınır ve tüm refresh token'lar revoke edilir.
        /// </summary>
        /// <returns>Çıkış sonucu</returns>
        /// <response code="200">Çıkış başarılı</response>
        /// <response code="401">Yetkisiz erişim</response>
        [HttpPost("logout")]
        [Authorize(Roles = "Courier")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Logout()
        {
            try
            {
                // 1. User ID'yi token'dan al
                var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                                 ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

                if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out var userId))
                {
                    return Unauthorized(new { Success = false, Message = "Geçersiz oturum." });
                }

                // 2. JTI'yi al (deny list için)
                var jti = User.FindFirst(JwtRegisteredClaimNames.Jti)?.Value ?? string.Empty;

                // 3. Token expiration'ı al
                var expClaim = User.FindFirst("exp")?.Value;
                DateTimeOffset expiration = DateTimeOffset.UtcNow.AddMinutes(5); // Fallback
                if (!string.IsNullOrWhiteSpace(expClaim) && long.TryParse(expClaim, out var expSeconds))
                {
                    expiration = DateTimeOffset.FromUnixTimeSeconds(expSeconds);
                }

                // 4. Çıkış işlemi
                var success = await _courierAuthService.LogoutAsync(userId, jti, expiration);

                if (success)
                {
                    _logger.LogInformation("Kurye çıkış başarılı, UserId: {UserId}", userId);
                    return Ok(new { Success = true, Message = "Başarıyla çıkış yapıldı." });
                }

                return StatusCode(StatusCodes.Status500InternalServerError, new 
                { 
                    Success = false, 
                    Message = "Çıkış sırasında bir hata oluştu." 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kurye çıkış endpoint hatası");
                return StatusCode(StatusCodes.Status500InternalServerError, new 
                { 
                    Success = false, 
                    Message = "Çıkış sırasında bir hata oluştu." 
                });
            }
        }

        /// <summary>
        /// Token yenileme endpoint'i.
        /// Mevcut (süresi dolmuş olabilir) access token ve geçerli refresh token ile yeni token çifti alınır.
        /// </summary>
        /// <param name="dto">Access token ve refresh token</param>
        /// <returns>Yeni JWT tokens</returns>
        /// <response code="200">Token yenileme başarılı</response>
        /// <response code="400">Geçersiz istek</response>
        /// <response code="401">Geçersiz veya süresi dolmuş refresh token</response>
        [HttpPost("refresh")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(CourierTokenRefreshResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> RefreshToken([FromBody] CourierTokenRefreshDto dto)
        {
            try
            {
                // 1. Input validation
                if (dto == null || string.IsNullOrWhiteSpace(dto.AccessToken) || string.IsNullOrWhiteSpace(dto.RefreshToken))
                {
                    return BadRequest(new { Success = false, Message = "Access token ve refresh token zorunludur." });
                }

                // 2. İstemci IP adresini al
                var ipAddress = GetClientIpAddress();

                // 3. Token yenileme işlemi
                var result = await _courierAuthService.RefreshTokenAsync(dto, ipAddress);

                if (result.Success)
                {
                    return Ok(result);
                }

                // Güvenlik ihlali veya oturum süresi dolmuşsa 401 dön
                if (result.Message.Contains("Güvenlik ihlali") || result.Message.Contains("Oturum süresi"))
                {
                    return Unauthorized(result);
                }

                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kurye token yenileme endpoint hatası");
                return StatusCode(StatusCodes.Status500InternalServerError, new 
                { 
                    Success = false, 
                    Message = "Token yenileme sırasında bir hata oluştu." 
                });
            }
        }

        /// <summary>
        /// Kurye şifre değiştirme endpoint'i.
        /// Kurye kendi şifresini değiştirir (mevcut şifre doğrulaması gerekir).
        /// </summary>
        /// <param name="dto">Mevcut ve yeni şifre bilgileri</param>
        /// <returns>İşlem sonucu</returns>
        /// <response code="200">Şifre değiştirme başarılı</response>
        /// <response code="400">Geçersiz istek veya şifre uyuşmazlığı</response>
        /// <response code="401">Yetkisiz erişim</response>
        [HttpPost("change-password")]
        [Authorize(Roles = "Courier")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> ChangePassword([FromBody] CourierChangePasswordDto dto)
        {
            try
            {
                // 1. Input validation
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    return BadRequest(new { Success = false, Message = string.Join(" ", errors) });
                }

                // 2. User ID'yi token'dan al
                var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out var userId))
                {
                    return Unauthorized(new { Success = false, Message = "Geçersiz oturum." });
                }

                // 3. Şifre değiştirme işlemi
                var (success, message) = await _courierAuthService.ChangePasswordAsync(userId, dto);

                if (success)
                {
                    _logger.LogInformation("Kurye şifre değiştirme başarılı, UserId: {UserId}", userId);
                    return Ok(new { Success = true, Message = message });
                }

                return BadRequest(new { Success = false, Message = message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kurye şifre değiştirme endpoint hatası");
                return StatusCode(StatusCodes.Status500InternalServerError, new 
                { 
                    Success = false, 
                    Message = "Şifre değiştirme sırasında bir hata oluştu." 
                });
            }
        }

        /// <summary>
        /// Mevcut kurye bilgilerini getir.
        /// JWT token'daki kullanıcı bilgilerine göre kurye detayları döndürülür.
        /// </summary>
        /// <returns>Kurye bilgileri</returns>
        /// <response code="200">Kurye bilgileri</response>
        /// <response code="401">Yetkisiz erişim</response>
        /// <response code="404">Kurye bulunamadı</response>
        [HttpGet("me")]
        [Authorize(Roles = "Courier")]
        [ProducesResponseType(typeof(CourierInfoDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCurrentCourier()
        {
            try
            {
                // 1. User ID'yi token'dan al
                var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out var userId))
                {
                    return Unauthorized(new { Success = false, Message = "Geçersiz oturum." });
                }

                // 2. Kurye bilgilerini getir
                var courierInfo = await _courierAuthService.GetCourierByUserIdAsync(userId);

                if (courierInfo == null)
                {
                    return NotFound(new { Success = false, Message = "Kurye bulunamadı." });
                }

                return Ok(new { Success = true, Courier = courierInfo });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kurye bilgileri getirme endpoint hatası");
                return StatusCode(StatusCodes.Status500InternalServerError, new 
                { 
                    Success = false, 
                    Message = "Bilgiler alınırken bir hata oluştu." 
                });
            }
        }

        /// <summary>
        /// Kurye oturumunun geçerliliğini kontrol eder.
        /// Token doğrulaması ve kurye kaydı kontrolü yapar.
        /// </summary>
        /// <returns>Oturum durumu</returns>
        /// <response code="200">Oturum geçerli</response>
        /// <response code="401">Oturum geçersiz</response>
        [HttpGet("validate")]
        [Authorize(Roles = "Courier")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> ValidateSession()
        {
            try
            {
                // 1. User ID'yi token'dan al
                var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out var userId))
                {
                    return Unauthorized(new { Success = false, Message = "Geçersiz oturum." });
                }

                // 2. Kurye kaydı kontrolü
                var isValid = await _courierAuthService.ValidateCourierAsync(userId);

                if (!isValid)
                {
                    return Unauthorized(new { Success = false, Message = "Kurye kaydı bulunamadı veya geçersiz." });
                }

                return Ok(new { Success = true, Message = "Oturum geçerli.", UserId = userId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kurye oturum doğrulama endpoint hatası");
                return Unauthorized(new { Success = false, Message = "Oturum doğrulama hatası." });
            }
        }

        #region Private Helpers

        /// <summary>
        /// İstemci IP adresini alır.
        /// X-Forwarded-For header'ı (proxy arkası için) veya doğrudan bağlantı IP'si kullanılır.
        /// </summary>
        private string? GetClientIpAddress()
        {
            // Proxy/Load balancer arkasında X-Forwarded-For header'ını kontrol et
            var forwardedFor = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(forwardedFor))
            {
                // X-Forwarded-For birden fazla IP içerebilir, ilkini al
                return forwardedFor.Split(',').FirstOrDefault()?.Trim();
            }

            return HttpContext.Connection.RemoteIpAddress?.ToString();
        }

        #endregion
    }
}
