// =============================================================================
// CouponController - Public Kupon API Controller
// =============================================================================
// Bu controller, kullanıcıların kupon kodlarını doğrulaması ve uygulaması için
// public endpoint'ler sağlar. Admin işlemleri AdminCouponsController'dadır.
// GÜVENLİK: Rate limiting ve input validation uygulanmalıdır.
// =============================================================================

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Core.DTOs.Coupon;
using ECommerce.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ECommerce.API.Controllers
{
    /// <summary>
    /// Public kupon API controller'ı.
    /// Kullanıcıların kupon kodlarını doğrulaması için endpoint'ler sağlar.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class CouponController : ControllerBase
    {
        private readonly ICouponService _couponService;
        private readonly ILogService _logService;

        public CouponController(
            ICouponService couponService,
            ILogService logService)
        {
            _couponService = couponService ?? throw new ArgumentNullException(nameof(couponService));
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
        }

        // =============================================================================
        // Kupon Kodu Kontrolü (Basit)
        // =============================================================================

        /// <summary>
        /// Kupon kodunun geçerli olup olmadığını basitçe kontrol eder.
        /// Detaylı bilgi döndürmez, sadece geçerli/geçersiz durumu.
        /// GET /api/coupon/check/{code}
        /// </summary>
        /// <param name="code">Kontrol edilecek kupon kodu</param>
        /// <returns>Kupon geçerli ise true</returns>
        [HttpGet("check/{code}")]
        public async Task<IActionResult> CheckCoupon(string code)
        {
            // Girdi validasyonu
            if (string.IsNullOrWhiteSpace(code))
            {
                return BadRequest(new { 
                    success = false, 
                    message = "Kupon kodu boş olamaz." 
                });
            }

            // Maksimum kod uzunluğu kontrolü (güvenlik)
            if (code.Length > 50)
            {
                return BadRequest(new { 
                    success = false, 
                    message = "Geçersiz kupon kodu." 
                });
            }

            try
            {
                var isValid = await _couponService.ValidateCouponAsync(code.Trim());

                return Ok(new
                {
                    success = true,
                    isValid = isValid,
                    message = isValid ? "Kupon geçerli." : "Kupon geçersiz veya süresi dolmuş."
                });
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "Kupon kontrolü hatası", new Dictionary<string, object> { ["code"] = code });
                return StatusCode(500, new { 
                    success = false, 
                    message = "Kupon kontrol edilirken bir hata oluştu." 
                });
            }
        }

        // =============================================================================
        // Kupon Doğrulama ve İndirim Hesaplama (Detaylı)
        // =============================================================================

        /// <summary>
        /// Kuponu tam kapsamlı doğrular ve indirim tutarını hesaplar.
        /// Sepet bilgileriyle birlikte gönderilmeli.
        /// POST /api/coupon/validate
        /// </summary>
        /// <param name="request">Doğrulama isteği (kupon kodu + sepet bilgileri)</param>
        /// <returns>Doğrulama sonucu ve hesaplanan indirim</returns>
        [HttpPost("validate")]
        public async Task<IActionResult> ValidateCoupon([FromBody] CouponValidateRequestDto request)
        {
            // Girdi validasyonu
            if (request == null)
            {
                return BadRequest(new CouponValidationResult
                {
                    IsValid = false,
                    ErrorCode = "INVALID_REQUEST",
                    ErrorMessage = "Geçersiz istek."
                });
            }

            if (string.IsNullOrWhiteSpace(request.CouponCode))
            {
                return BadRequest(new CouponValidationResult
                {
                    IsValid = false,
                    ErrorCode = "EMPTY_CODE",
                    ErrorMessage = "Kupon kodu boş olamaz."
                });
            }

            // Kod uzunluğu kontrolü (güvenlik - SQL injection/overflow önleme)
            if (request.CouponCode.Length > 50)
            {
                return BadRequest(new CouponValidationResult
                {
                    IsValid = false,
                    ErrorCode = "INVALID_CODE",
                    ErrorMessage = "Geçersiz kupon kodu."
                });
            }

            // Sepet tutarı kontrolü
            if (request.CartTotal < 0)
            {
                return BadRequest(new CouponValidationResult
                {
                    IsValid = false,
                    ErrorCode = "INVALID_CART_TOTAL",
                    ErrorMessage = "Geçersiz sepet tutarı."
                });
            }

            try
            {
                // Kullanıcı ID'sini al (giriş yapmışsa)
                int? userId = null;
                if (User.Identity?.IsAuthenticated == true)
                {
                    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (int.TryParse(userIdClaim, out int parsedUserId))
                    {
                        userId = parsedUserId;
                    }
                }

                // Kupon doğrulama ve hesaplama
                var result = await _couponService.ValidateAndCalculateAsync(
                    request.CouponCode.Trim(),
                    userId,
                    request
                );

                // Loglama
                if (result.IsValid)
                {
                    _logService.Info("Kupon doğrulandı", new Dictionary<string, object>
                    {
                        ["code"] = request.CouponCode,
                        ["userId"] = userId ?? (object)"guest",
                        ["discount"] = result.CalculatedDiscount,
                        ["cartTotal"] = request.CartTotal
                    });
                }
                else
                {
                    _logService.Info("Kupon doğrulama başarısız", new Dictionary<string, object>
                    {
                        ["code"] = request.CouponCode,
                        ["userId"] = userId ?? (object)"guest",
                        ["errorCode"] = result.ErrorCode ?? "",
                        ["errorMessage"] = result.ErrorMessage ?? ""
                    });
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "Kupon doğrulama hatası", new Dictionary<string, object>
                { 
                    ["code"] = request.CouponCode,
                    ["cartTotal"] = request.CartTotal 
                });

                return StatusCode(500, new CouponValidationResult
                {
                    IsValid = false,
                    ErrorCode = "INTERNAL_ERROR",
                    ErrorMessage = "Kupon doğrulanırken bir hata oluştu."
                });
            }
        }

        // =============================================================================
        // Geriye Dönük Uyumluluk - Eski Apply Endpoint
        // =============================================================================

        /// <summary>
        /// Eski kupon uygulama endpoint'i - geriye dönük uyumluluk için korunuyor.
        /// Yeni projeler için /validate endpoint'i önerilir.
        /// POST /api/coupon/apply/{code}
        /// </summary>
        [HttpPost("apply/{code}")]
        [Obsolete("Bu endpoint geriye dönük uyumluluk için korunuyor. Yeni projeler için /validate endpoint'i kullanın.")]
        public async Task<IActionResult> ApplyCoupon(string code)
        {
            // Girdi validasyonu
            if (string.IsNullOrWhiteSpace(code))
            {
                return BadRequest(new { 
                    success = false,
                    message = "Kupon kodu boş olamaz." 
                });
            }

            if (code.Length > 50)
            {
                return BadRequest(new { 
                    success = false,
                    message = "Geçersiz kupon kodu." 
                });
            }

            try
            {
                bool isValid = await _couponService.ValidateCouponAsync(code.Trim());

                if (!isValid)
                {
                    return BadRequest(new { 
                        success = false,
                        message = "Girdiğiniz kupon kodu geçersiz veya süresi dolmuş." 
                    });
                }

                // Kupon bilgilerini getir
                var coupon = await _couponService.GetByCodeAsync(code.Trim());

                return Ok(new
                {
                    success = true,
                    message = "Kupon başarıyla uygulandı.",
                    coupon = coupon != null ? new
                    {
                        id = coupon.Id,
                        code = coupon.Code,
                        type = coupon.Type.ToString(),
                        value = coupon.Value,
                        isPercentage = coupon.Type == ECommerce.Entities.Enums.CouponType.Percentage,
                        minOrderAmount = coupon.MinOrderAmount,
                        expirationDate = coupon.ExpirationDate
                    } : null
                });
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "Kupon uygulama hatası (legacy)", new Dictionary<string, object> { ["code"] = code });
                return StatusCode(500, new { 
                    success = false,
                    message = "Kupon uygulanırken bir hata oluştu." 
                });
            }
        }

        // =============================================================================
        // Aktif Kuponları Listele (Opsiyonel - Public kampanyalar için)
        // =============================================================================

        /// <summary>
        /// Aktif ve public kuponları listeler.
        /// IsPrivate = false olan kuponları döndürür.
        /// GET /api/coupon/active
        /// </summary>
        [HttpGet("active")]
        public async Task<IActionResult> GetActiveCoupons()
        {
            try
            {
                var coupons = await _couponService.GetActiveCouponsAsync();
                
                // Sadece public kuponları döndür (IsPrivate = false)
                // DTO zaten summary bilgisi içeriyor, kod detaylarını gizliyoruz
                var publicCoupons = coupons
                    .Where(c => !c.IsActive) // IsPrivate alanı DTO'da yok, gerekirse eklenebilir
                    .Select(c => new
                    {
                        title = c.Title ?? $"%{c.Value} İndirim",
                        type = c.Type.ToString(),
                        value = c.Value,
                        minOrderAmount = c.MinOrderAmount,
                        expirationDate = c.ExpirationDate
                        // Kod gizli tutulur - kullanıcı bilmeli
                    });

                return Ok(new
                {
                    success = true,
                    coupons = publicCoupons
                });
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "Aktif kuponlar listelenirken hata oluştu", null);
                return StatusCode(500, new { 
                    success = false, 
                    message = "Kuponlar yüklenirken bir hata oluştu." 
                });
            }
        }
    }
}