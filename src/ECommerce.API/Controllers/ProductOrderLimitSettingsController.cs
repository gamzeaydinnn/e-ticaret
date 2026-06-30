using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ECommerce.Core.Interfaces;
using System.Security.Claims;

namespace ECommerce.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [IgnoreAntiforgeryToken]
    public class ProductOrderLimitSettingsController : ControllerBase
    {
        private readonly IProductOrderLimitSettingsService _settingsService;
        private readonly ILogger<ProductOrderLimitSettingsController> _logger;

        public ProductOrderLimitSettingsController(
            IProductOrderLimitSettingsService settingsService,
            ILogger<ProductOrderLimitSettingsController> logger)
        {
            _settingsService = settingsService;
            _logger = logger;
        }

        [HttpGet("settings")]
        [AllowAnonymous]
        public async Task<IActionResult> GetActiveSettings(CancellationToken cancellationToken)
        {
            try
            {
                var settings = await _settingsService.GetActiveSettingsAsync(cancellationToken);
                return Ok(settings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sipariş limit ayarları getirilemedi");
                return StatusCode(500, new { message = "Sipariş limit ayarları yüklenemedi." });
            }
        }

        [HttpGet("admin/settings")]
        [Authorize]
        public async Task<IActionResult> GetAdminSettings(CancellationToken cancellationToken)
        {
            try
            {
                var settings = await _settingsService.GetSettingsForAdminAsync(cancellationToken);
                return Ok(settings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ADMIN] Sipariş limit ayarları getirilemedi");
                return StatusCode(500, new { message = "Sipariş limit ayarları yüklenemedi." });
            }
        }

        [HttpPut("admin/settings")]
        [Authorize]
        public async Task<IActionResult> UpdateSettings(
            [FromBody] ProductOrderLimitSettingsUpdateDto request,
            CancellationToken cancellationToken)
        {
            if (request == null)
            {
                return BadRequest(new { message = "Güncelleme verisi gerekli." });
            }

            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(new { message = "Kullanıcı bilgisi alınamadı." });
            }

            try
            {
                var updated = await _settingsService.UpdateSettingsAsync(
                    request,
                    userId.Value,
                    GetCurrentUserName(),
                    cancellationToken);

                return Ok(new { message = "Sipariş limit ayarları güncellendi.", data = updated });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ADMIN] Sipariş limit ayarları güncellenemedi");
                return StatusCode(500, new { message = "Sipariş limit ayarları güncellenemedi." });
            }
        }

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                              ?? User.FindFirst("sub")?.Value
                              ?? User.FindFirst("userId")?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : null;
        }

        private string GetCurrentUserName() =>
            User.FindFirst(ClaimTypes.Name)?.Value
            ?? User.FindFirst("name")?.Value
            ?? User.FindFirst(ClaimTypes.Email)?.Value
            ?? "Admin";
    }
}
