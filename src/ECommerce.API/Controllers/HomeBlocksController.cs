using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using System.Collections.Generic;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Core.DTOs.HomeBlock;

namespace ECommerce.API.Controllers
{
    /// <summary>
    /// Ana Sayfa Ürün Blokları Controller - Public Endpoint'ler
    /// ------------------------------------------------
    /// Ana sayfa için ürün bloklarını sunar.
    /// Herkes erişebilir (AllowAnonymous).
    /// 
    /// Endpoint'ler:
    /// GET /api/homeblocks - Ana sayfa için aktif blokları getirir
    /// GET /api/homeblocks/{slug} - Slug'a göre tek blok (Tümünü Gör)
    /// GET /api/homeblocks/preview - Blok tipi preview (admin için)
    /// </summary>
    [ApiController]
    [Route("api/homeblocks")]
    [IgnoreAntiforgeryToken]
    [AllowAnonymous]
    public class HomeBlocksController : ControllerBase
    {
        private readonly IHomeBlockService _homeBlockService;
        private readonly ILogger<HomeBlocksController> _logger;

        public HomeBlocksController(
            IHomeBlockService homeBlockService,
            ILogger<HomeBlocksController> logger)
        {
            _homeBlockService = homeBlockService;
            _logger = logger;
        }

        /// <summary>
        /// Ana sayfa için aktif blokları getirir
        /// Her blok poster ve ürünleriyle birlikte döner
        /// DisplayOrder'a göre sıralı, tarih kontrolü yapılmış
        /// </summary>
        /// <returns>Aktif blok listesi (ürünlerle birlikte)</returns>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<HomeProductBlockDto>), 200)]
        public async Task<IActionResult> GetActiveBlocks()
        {
            _logger.LogInformation("🏠 Ana sayfa blokları isteniyor");
            
            var blocks = await _homeBlockService.GetActiveBlocksForHomepageAsync();
            
            _logger.LogInformation("✅ {Count} aktif blok döndürüldü", 
                blocks is ICollection<HomeProductBlockDto> collection ? collection.Count : 0);
            
            return Ok(blocks);
        }

        /// <summary>
        /// Slug'a göre tek blok getirir - Tümünü Gör sayfası için
        /// Tüm ürünler döner (MaxProductCount sınırı yok)
        /// </summary>
        /// <param name="slug">Blok slug'ı (örn: indirimli-urunler)</param>
        /// <returns>Blok detayı ve tüm ürünleri</returns>
        [HttpGet("{slug}")]
        [ProducesResponseType(typeof(HomeProductBlockDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetBlockBySlug(string slug)
        {
            _logger.LogInformation("🔍 Blok isteniyor: {Slug}", slug);
            
            var block = await _homeBlockService.GetBlockBySlugAsync(slug);
            
            if (block == null)
            {
                _logger.LogWarning("⚠️ Blok bulunamadı: {Slug}", slug);
                return NotFound(new { message = $"'{slug}' bloğu bulunamadı" });
            }
            
            _logger.LogInformation("✅ Blok döndürüldü: {Name}", block.Name);
            return Ok(block);
        }

        [HttpGet("{slug}/products")]
        [ProducesResponseType(typeof(ECommerce.Core.DTOs.PagedResult<HomeBlockProductItemDto>), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetBlockProductsPaged(
            string slug,
            [FromQuery] int page = 1,
            [FromQuery] int size = 24)
        {
            _logger.LogInformation("📄 Blok ürünleri sayfalı isteniyor: {Slug}, Page={Page}, Size={Size}", slug, page, size);

            var pagedResult = await _homeBlockService.GetBlockBySlugPagedAsync(slug, page, size);
            if (pagedResult == null)
            {
                _logger.LogWarning("⚠️ Sayfalı blok ürünleri için blok bulunamadı: {Slug}", slug);
                return NotFound(new { message = $"'{slug}' bloğu bulunamadı" });
            }

            return Ok(pagedResult);
        }

        /// <summary>
        /// Blok tipi önizlemesi - Admin için
        /// Belirli bir blok tipinin ürünlerini önizler
        /// </summary>
        /// <param name="blockType">Blok tipi: manual, category, discounted, newest, bestseller</param>
        /// <param name="categoryId">Kategori ID (sadece category tipi için)</param>
        /// <param name="maxCount">Maksimum ürün sayısı (varsayılan: 6)</param>
        /// <returns>Ürün listesi</returns>
        [HttpGet("preview")]
        [ProducesResponseType(typeof(IEnumerable<HomeBlockProductItemDto>), 200)]
        public async Task<IActionResult> PreviewBlockProducts(
            [FromQuery] string blockType = "newest",
            [FromQuery] int? categoryId = null,
            [FromQuery] int maxCount = 6)
        {
            _logger.LogInformation("👁️ Blok önizleme: Type={Type}, CategoryId={CategoryId}", 
                blockType, categoryId);
            
            var products = await _homeBlockService.GetProductsByBlockTypeAsync(blockType, categoryId, maxCount);
            
            return Ok(products);
        }
    }
}
