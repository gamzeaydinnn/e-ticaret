// ==========================================================================
// CartSettingsController.cs - Sepet Ayarları API Controller'ı
// ==========================================================================
// Minimum sepet tutarı ayarlarının CRUD işlemleri için API endpoint'leri.
// Public: Sepet/checkout sayfası için ayar sorgulama
// Admin: Ayar güncelleme ve yönetim
// ==========================================================================

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ECommerce.Core.Interfaces;
using System.Security.Claims;

namespace ECommerce.API.Controllers
{
    /// <summary>
    /// Sepet ayarları yönetimi API controller'ı.
    /// Minimum sepet tutarı sorgulama ve admin güncelleme endpoint'leri.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [IgnoreAntiforgeryToken]
    public class CartSettingsController : ControllerBase
    {
        // ═══════════════════════════════════════════════════════════════════════════════
        // BAĞIMLILIKLAR
        // ═══════════════════════════════════════════════════════════════════════════════

        private readonly ICartSettingsService _cartSettingsService;
        private readonly ILogger<CartSettingsController> _logger;

        public CartSettingsController(
            ICartSettingsService cartSettingsService,
            ILogger<CartSettingsController> logger)
        {
            _cartSettingsService = cartSettingsService ?? throw new ArgumentNullException(nameof(cartSettingsService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // ═══════════════════════════════════════════════════════════════════════════════
        // PUBLIC ENDPOINT'LER (Herkes Erişebilir)
        // Sepet ve checkout sayfaları için
        // ═══════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Aktif sepet ayarlarını getirir.
        /// Sepet ve checkout sayfalarında minimum tutar kontrolü için kullanılır.
        /// </summary>
        /// <returns>Aktif sepet ayarları</returns>
        [HttpGet("settings")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(CartSettingsDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetActiveSettings()
        {
            _logger.LogDebug("Aktif sepet ayarları isteniyor");

            try
            {
                var settings = await _cartSettingsService.GetActiveSettingsAsync();
                return Ok(settings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sepet ayarları getirilirken hata oluştu");
                return StatusCode(500, new { message = "Sepet ayarları yüklenirken bir hata oluştu" });
            }
        }

        // ═══════════════════════════════════════════════════════════════════════════════
        // ADMIN ENDPOINT'LER (Yetkilendirme Gerekli)
        // Sepet ayarları yönetimi için
        // ═══════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Admin paneli için sepet ayarlarını getirir (cache'siz, taze veri).
        /// </summary>
        [HttpGet("admin/settings")]
        [Authorize]
        [ProducesResponseType(typeof(CartSettingsDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSettingsAdmin()
        {
            _logger.LogInformation("[ADMIN] Sepet ayarları isteniyor");

            try
            {
                var settings = await _cartSettingsService.GetSettingsForAdminAsync();
                return Ok(settings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ADMIN] Sepet ayarları getirilirken hata oluştu");
                return StatusCode(500, new { message = "Sepet ayarları yüklenirken bir hata oluştu" });
            }
        }

        /// <summary>
        /// Sepet ayarlarını günceller (minimum tutar, mesaj, aktiflik).
        /// </summary>
        [HttpPut("admin/settings")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateSettings([FromBody] CartSettingsUpdateRequest request)
        {
            if (request == null)
            {
                return BadRequest(new { message = "Güncelleme verisi gerekli" });
            }

            // Negatif tutar kontrolü
            if (request.MinimumCartAmount.HasValue && request.MinimumCartAmount.Value < 0)
            {
                return BadRequest(new { message = "Minimum sepet tutarı negatif olamaz" });
            }

            // Admin bilgilerini al
            var userId = GetCurrentUserId();
            var userName = GetCurrentUserName();

            if (!userId.HasValue)
            {
                return Unauthorized(new { message = "Kullanıcı bilgisi alınamadı" });
            }

            _logger.LogInformation(
                "[ADMIN] Sepet ayarları güncelleniyor. Admin: {UserName}", userName);

            try
            {
                var updateDto = new CartSettingsUpdateDto
                {
                    MinimumCartAmount = request.MinimumCartAmount,
                    IsMinimumCartAmountActive = request.IsMinimumCartAmountActive,
                    MinimumCartAmountMessage = request.MinimumCartAmountMessage
                };

                var result = await _cartSettingsService.UpdateSettingsAsync(updateDto, userId.Value, userName);

                if (!result)
                {
                    return BadRequest(new { message = "Sepet ayarları güncellenemedi" });
                }

                _logger.LogInformation(
                    "[ADMIN] Sepet ayarları güncellendi. MinAmount: {Amount}, Active: {Active}, Admin: {UserName}",
                    request.MinimumCartAmount, request.IsMinimumCartAmountActive, userName);

                // Güncellenmiş ayarları döndür
                var updatedSettings = await _cartSettingsService.GetSettingsForAdminAsync();
                return Ok(new
                {
                    message = "Sepet ayarları başarıyla güncellendi",
                    data = updatedSettings
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ADMIN] Sepet ayarları güncellenirken hata");
                return StatusCode(500, new { message = "Sepet ayarları güncellenirken bir hata oluştu" });
            }
        }

        // ═══════════════════════════════════════════════════════════════════════════════
        // YARDIMCI METODLAR
        // ═══════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// JWT token'dan kullanıcı ID'sini alır.
        /// </summary>
        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                           ?? User.FindFirst("sub")?.Value
                           ?? User.FindFirst("userId")?.Value;

            if (int.TryParse(userIdClaim, out var userId))
            {
                return userId;
            }

            return null;
        }

        /// <summary>
        /// JWT token'dan kullanıcı adını alır.
        /// </summary>
        private string GetCurrentUserName()
        {
            return User.FindFirst(ClaimTypes.Name)?.Value
                ?? User.FindFirst("name")?.Value
                ?? User.FindFirst(ClaimTypes.Email)?.Value
                ?? "Admin";
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // REQUEST DTO'LAR
    // Controller'a özgü request modelleri
    // ═══════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Sepet ayarları güncelleme isteği.
    /// Tüm alanlar opsiyonel - sadece gönderilen alanlar güncellenir.
    /// </summary>
    public class CartSettingsUpdateRequest
    {
        /// <summary>
        /// Minimum sepet tutarı (TL) - null ise değişmez
        /// </summary>
        public decimal? MinimumCartAmount { get; set; }

        /// <summary>
        /// Minimum tutar kuralı aktif mi - null ise değişmez
        /// </summary>
        public bool? IsMinimumCartAmountActive { get; set; }

        /// <summary>
        /// Müşteriye gösterilecek uyarı mesajı - null ise değişmez
        /// </summary>
        public string? MinimumCartAmountMessage { get; set; }
    }
}
