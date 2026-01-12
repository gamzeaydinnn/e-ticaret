using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Core.DTOs;

namespace ECommerce.API.Controllers
{
    /// <summary>
    /// Public banner endpoint'leri - ana sayfa için
    /// Sadece aktif banner'ları döndürür
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [IgnoreAntiforgeryToken]
    [AllowAnonymous]
    public class BannersController : ControllerBase
    {
        private readonly IBannerService _bannerService;
        private readonly ILogger<BannersController> _logger;
        
        public BannersController(IBannerService bannerService, ILogger<BannersController> logger)
        {
            _bannerService = bannerService;
            _logger = logger;
        }

        /// <summary>
        /// Tüm aktif banner'ları getirir
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            _logger.LogInformation("🔍 BannersController.GetAll çağrıldı");
            var banners = await _bannerService.GetActiveAsync();
            _logger.LogInformation("✅ {Count} aktif banner döndürüldü", banners.Count());
            return Ok(banners);
        }

        /// <summary>
        /// Slider banner'larını getirir (ana sayfa karusel için)
        /// </summary>
        [HttpGet("slider")]
        public async Task<IActionResult> GetSliderBanners()
        {
            _logger.LogInformation("🎠 Slider banner'ları isteniyor");
            var banners = await _bannerService.GetByTypeAsync("slider");
            _logger.LogInformation("✅ {Count} slider banner döndürüldü", banners.Count());
            return Ok(banners);
        }

        /// <summary>
        /// Promo banner'larını getirir (promosyon kartları için)
        /// </summary>
        [HttpGet("promo")]
        public async Task<IActionResult> GetPromoBanners()
        {
            _logger.LogInformation("🏷️ Promo banner'ları isteniyor");
            var banners = await _bannerService.GetByTypeAsync("promo");
            _logger.LogInformation("✅ {Count} promo banner döndürüldü", banners.Count());
            return Ok(banners);
        }

        /// <summary>
        /// Genel banner'ları getirir
        /// </summary>
        [HttpGet("general")]
        public async Task<IActionResult> GetGeneralBanners()
        {
            _logger.LogInformation("📢 Genel banner'lar isteniyor");
            var banners = await _bannerService.GetByTypeAsync("banner");
            _logger.LogInformation("✅ {Count} genel banner döndürüldü", banners.Count());
            return Ok(banners);
        }

        /// <summary>
        /// Tipe göre banner'ları getirir
        /// </summary>
        [HttpGet("type/{type}")]
        public async Task<IActionResult> GetByType(string type)
        {
            _logger.LogInformation("📋 {Type} tipindeki banner'lar isteniyor", type);
            var banners = await _bannerService.GetByTypeAsync(type);
            _logger.LogInformation("✅ {Count} {Type} banner döndürüldü", banners.Count(), type);
            return Ok(banners);
        }

        /// <summary>
        /// ID'ye göre banner getirir
        /// </summary>
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var banner = await _bannerService.GetByIdAsync(id);
            if (banner == null) 
            {
                _logger.LogWarning("⚠️ Banner #{Id} bulunamadı", id);
                return NotFound(new { message = $"Banner #{id} bulunamadı" });
            }
            return Ok(banner);
        }
    }
}
