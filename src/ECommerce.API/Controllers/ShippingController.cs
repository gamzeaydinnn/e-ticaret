// ==========================================================================
// ShippingController.cs - Kargo Ücreti API Controller'ı
// ==========================================================================
// Kargo ayarlarının CRUD işlemleri için API endpoint'leri.
// Public: Sepet sayfası için fiyat sorgulama
// Admin: Fiyat güncelleme ve yönetim
// ==========================================================================

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ECommerce.Core.Interfaces;
using System.Security.Claims;

namespace ECommerce.API.Controllers
{
    /// <summary>
    /// Kargo ücreti yönetimi API controller'ı.
    /// Araç tipine göre (motorcycle/car) dinamik fiyatlandırma sağlar.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [IgnoreAntiforgeryToken]
    public class ShippingController : ControllerBase
    {
        // ═══════════════════════════════════════════════════════════════════════════════
        // BAĞIMLILIKLAR
        // ═══════════════════════════════════════════════════════════════════════════════

        private readonly IShippingService _shippingService;
        private readonly ILogger<ShippingController> _logger;

        public ShippingController(
            IShippingService shippingService,
            ILogger<ShippingController> logger)
        {
            _shippingService = shippingService ?? throw new ArgumentNullException(nameof(shippingService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // ═══════════════════════════════════════════════════════════════════════════════
        // PUBLIC ENDPOINT'LER (Herkes Erişebilir)
        // Sepet ve ödeme sayfaları için
        // ═══════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Aktif kargo seçeneklerini getirir.
        /// Sepet sayfasında müşteriye gösterilecek seçenekler.
        /// </summary>
        /// <returns>Aktif kargo ayarları listesi (sıralı)</returns>
        /// <response code="200">Başarılı - Kargo seçenekleri döndürüldü</response>
        [HttpGet("settings")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(IEnumerable<ShippingSettingDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetActiveSettings()
        {
            _logger.LogDebug("📦 Aktif kargo ayarları isteniyor");

            try
            {
                var settings = await _shippingService.GetActiveSettingsAsync();
                
                _logger.LogDebug("✅ {Count} aktif kargo seçeneği döndürüldü", settings.Count());
                return Ok(settings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kargo ayarları getirilirken hata oluştu");
                return StatusCode(500, new { message = "Kargo ayarları yüklenirken bir hata oluştu" });
            }
        }

        /// <summary>
        /// Belirli bir araç tipinin kargo ücretini getirir.
        /// </summary>
        /// <param name="vehicleType">Araç tipi: "motorcycle" veya "car"</param>
        /// <returns>Kargo ücreti (TL)</returns>
        /// <response code="200">Başarılı - Fiyat döndürüldü</response>
        /// <response code="404">Araç tipi bulunamadı</response>
        [HttpGet("price/{vehicleType}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetPriceByVehicleType(string vehicleType)
        {
            if (string.IsNullOrWhiteSpace(vehicleType))
            {
                return BadRequest(new { message = "Araç tipi belirtilmedi" });
            }

            _logger.LogDebug("💰 Kargo fiyatı sorgulanıyor: {VehicleType}", vehicleType);

            try
            {
                var price = await _shippingService.GetPriceByVehicleTypeAsync(vehicleType);

                if (!price.HasValue)
                {
                    _logger.LogWarning("Araç tipi için kargo fiyatı bulunamadı: {VehicleType}", vehicleType);
                    return NotFound(new { message = $"'{vehicleType}' araç tipi için kargo ayarı bulunamadı" });
                }

                _logger.LogDebug("✅ Kargo fiyatı: {VehicleType} = {Price} TL", vehicleType, price.Value);
                
                return Ok(new 
                { 
                    vehicleType = vehicleType.ToLowerInvariant(),
                    price = price.Value,
                    currency = "TRY"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kargo fiyatı sorgulanırken hata: {VehicleType}", vehicleType);
                return StatusCode(500, new { message = "Kargo fiyatı alınırken bir hata oluştu" });
            }
        }

        /// <summary>
        /// Araç tipine göre kargo ayarı detayını getirir.
        /// </summary>
        [HttpGet("settings/type/{vehicleType}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ShippingSettingDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetSettingByVehicleType(string vehicleType)
        {
            if (string.IsNullOrWhiteSpace(vehicleType))
            {
                return BadRequest(new { message = "Araç tipi belirtilmedi" });
            }

            try
            {
                var setting = await _shippingService.GetByVehicleTypeAsync(vehicleType);

                if (setting == null)
                {
                    return NotFound(new { message = $"'{vehicleType}' araç tipi için kargo ayarı bulunamadı" });
                }

                return Ok(setting);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kargo ayarı sorgulanırken hata: {VehicleType}", vehicleType);
                return StatusCode(500, new { message = "Kargo ayarı alınırken bir hata oluştu" });
            }
        }

        // ═══════════════════════════════════════════════════════════════════════════════
        // ADMIN ENDPOINT'LER (Yetkilendirme Gerekli)
        // Kargo ayarları yönetimi için
        // ═══════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Tüm kargo ayarlarını getirir (aktif/pasif dahil).
        /// Admin paneli için kullanılır.
        /// </summary>
        [HttpGet("admin/settings")]
        [Authorize]
        [ProducesResponseType(typeof(IEnumerable<ShippingSettingDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllSettingsAdmin()
        {
            _logger.LogInformation("🔧 [ADMIN] Tüm kargo ayarları isteniyor");

            try
            {
                var settings = await _shippingService.GetAllSettingsAsync();
                
                _logger.LogInformation("✅ [ADMIN] {Count} kargo ayarı döndürüldü", settings.Count());
                return Ok(settings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ADMIN] Kargo ayarları getirilirken hata oluştu");
                return StatusCode(500, new { message = "Kargo ayarları yüklenirken bir hata oluştu" });
            }
        }

        /// <summary>
        /// Belirli bir kargo ayarını ID ile getirir.
        /// </summary>
        [HttpGet("admin/settings/{id:int}")]
        [Authorize]
        [ProducesResponseType(typeof(ShippingSettingDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetSettingById(int id)
        {
            try
            {
                var setting = await _shippingService.GetByIdAsync(id);

                if (setting == null)
                {
                    return NotFound(new { message = $"ID: {id} için kargo ayarı bulunamadı" });
                }

                return Ok(setting);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ADMIN] Kargo ayarı sorgulanırken hata: {Id}", id);
                return StatusCode(500, new { message = "Kargo ayarı alınırken bir hata oluştu" });
            }
        }

        /// <summary>
        /// Kargo ayarını günceller.
        /// </summary>
        /// <param name="id">Güncellenecek ayar ID'si</param>
        /// <param name="request">Güncellenecek alanlar</param>
        [HttpPut("admin/settings/{id:int}")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateSetting(int id, [FromBody] ShippingSettingUpdateRequest request)
        {
            if (request == null)
            {
                return BadRequest(new { message = "Güncelleme verisi gerekli" });
            }

            // Negatif fiyat kontrolü
            if (request.Price.HasValue && request.Price.Value < 0)
            {
                return BadRequest(new { message = "Kargo ücreti negatif olamaz" });
            }

            // Admin bilgilerini al
            var userId = GetCurrentUserId();
            var userName = GetCurrentUserName();

            if (!userId.HasValue)
            {
                return Unauthorized(new { message = "Kullanıcı bilgisi alınamadı" });
            }

            _logger.LogInformation(
                "🔧 [ADMIN] Kargo ayarı güncelleniyor. Id: {Id}, Admin: {UserName}", 
                id, userName);

            try
            {
                // DTO'ya dönüştür
                var updateDto = new ShippingSettingUpdateDto
                {
                    Price = request.Price,
                    DisplayName = request.DisplayName,
                    EstimatedDeliveryTime = request.EstimatedDeliveryTime,
                    Description = request.Description,
                    SortOrder = request.SortOrder,
                    MaxWeight = request.MaxWeight,
                    IsActive = request.IsActive
                };

                var result = await _shippingService.UpdateSettingAsync(id, updateDto, userId.Value, userName);

                if (!result)
                {
                    return NotFound(new { message = $"ID: {id} için kargo ayarı bulunamadı veya güncellenemedi" });
                }

                _logger.LogInformation(
                    "✅ [ADMIN] Kargo ayarı güncellendi. Id: {Id}, Yeni Fiyat: {Price}, Admin: {UserName}",
                    id, request.Price, userName);

                // Güncellenmiş ayarı döndür
                var updatedSetting = await _shippingService.GetByIdAsync(id);
                return Ok(new 
                { 
                    message = "Kargo ayarı başarıyla güncellendi",
                    data = updatedSetting
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ADMIN] Kargo ayarı güncellenirken hata: {Id}", id);
                return StatusCode(500, new { message = "Kargo ayarı güncellenirken bir hata oluştu" });
            }
        }

        /// <summary>
        /// Kargo ayarının aktif/pasif durumunu değiştirir.
        /// </summary>
        [HttpPatch("admin/settings/{id:int}/toggle")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ToggleActive(int id, [FromBody] ToggleActiveRequest request)
        {
            var userId = GetCurrentUserId();
            var userName = GetCurrentUserName();

            if (!userId.HasValue)
            {
                return Unauthorized(new { message = "Kullanıcı bilgisi alınamadı" });
            }

            _logger.LogInformation(
                "🔧 [ADMIN] Kargo ayarı aktiflik değiştiriliyor. Id: {Id}, IsActive: {IsActive}, Admin: {UserName}",
                id, request.IsActive, userName);

            try
            {
                var result = await _shippingService.ToggleActiveAsync(id, request.IsActive, userId.Value, userName);

                if (!result)
                {
                    return NotFound(new { message = $"ID: {id} için kargo ayarı bulunamadı" });
                }

                return Ok(new { message = $"Kargo ayarı {(request.IsActive ? "aktif" : "pasif")} yapıldı" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ADMIN] Kargo ayarı aktiflik değiştirilirken hata: {Id}", id);
                return StatusCode(500, new { message = "İşlem sırasında bir hata oluştu" });
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
    /// Kargo ayarı güncelleme isteği.
    /// Tüm alanlar opsiyonel - sadece gönderilen alanlar güncellenir.
    /// </summary>
    public class ShippingSettingUpdateRequest
    {
        /// <summary>
        /// Yeni kargo ücreti (TL)
        /// </summary>
        public decimal? Price { get; set; }

        /// <summary>
        /// Görüntüleme adı (örn: "Motosiklet ile Teslimat")
        /// </summary>
        public string? DisplayName { get; set; }

        /// <summary>
        /// Tahmini teslimat süresi (örn: "30-45 dakika")
        /// </summary>
        public string? EstimatedDeliveryTime { get; set; }

        /// <summary>
        /// Açıklama
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Sıralama (küçük değer önce)
        /// </summary>
        public int? SortOrder { get; set; }

        /// <summary>
        /// Maksimum taşınabilir ağırlık (kg)
        /// </summary>
        public decimal? MaxWeight { get; set; }

        /// <summary>
        /// Aktif/Pasif durumu
        /// </summary>
        public bool? IsActive { get; set; }
    }

    /// <summary>
    /// Aktif/Pasif toggle isteği.
    /// </summary>
    public class ToggleActiveRequest
    {
        public bool IsActive { get; set; }
    }
}
