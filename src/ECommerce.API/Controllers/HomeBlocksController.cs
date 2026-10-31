using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using System.Collections.Generic;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Core.DTOs.HomeBlock;
using ECommerce.Core.Constants;

namespace ECommerce.API.Controllers
{
    /// <summary>
    /// Ana Sayfa Ürün Blokları Controller - Public & Analytics Endpoint'ler
    /// ------------------------------------------------
    /// Ana sayfa için ürün bloklarını, tıklama hedeflerini ve analitik takibini sunar.
    /// 
    /// Endpoint'ler:
    /// GET  /api/homeblocks - Ana sayfa için aktif blokları getirir (zenginleştirilmiş & önbellekli)
    /// GET  /api/homeblocks/{slug} - Slug'a göre tek blok (Tümünü Gör)
    /// GET  /api/homeblocks/{slug}/products - Sayfalı blok ürünleri
    /// GET  /api/homeblocks/preview - Blok tipi preview (admin için)
    /// POST /api/homeblocks/{id}/track-click - Poster tıklanmasını kaydeder (analitik)
    /// POST /api/homeblocks/{id}/track-view - Blok görüntülenmesini kaydeder (Intersection Observer)
    /// POST /api/homeblocks - Yeni blok oluştur (Admin)
    /// PUT  /api/homeblocks/{id} - Blok güncelle (Admin)
    /// </summary>
    [ApiController]
    [Route("api/homeblocks")]
    [IgnoreAntiforgeryToken]
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
        /// Her blok poster, tıklama hedefi ve ürünleriyle birlikte döner
        /// DisplayOrder'a göre sıralı, tarih kontrolü yapılmış, 5 dakika ResponseCache
        /// </summary>
        /// <returns>Aktif blok listesi (ürünlerle birlikte)</returns>
        [HttpGet]
        [AllowAnonymous]
        [ResponseCache(Duration = 300)] // 5 minutes
        [ProducesResponseType(typeof(IEnumerable<HomeProductBlockDto>), 200)]
        public async Task<IActionResult> GetActiveBlocks()
        {
            _logger.LogInformation("🏠 Ana sayfa blokları isteniyor");
            
            var blocks = await _homeBlockService.GetAllActiveBlocksAsync();
            
            _logger.LogInformation("✅ {Count} aktif blok döndürüldü", blocks.Count);
            
            return Ok(blocks);
        }

        /// <summary>
        /// POST /api/homeblocks/{id}/track-click - Poster tıklamasını kaydeder
        /// Async analitik endpoint'i, derhal 204 döner
        /// </summary>
        [HttpPost("{id:int}/track-click")]
        [AllowAnonymous]
        [ProducesResponseType(204)]
        public async Task<IActionResult> TrackClick(int id)
        {
            _ = _homeBlockService.TrackPosterClickAsync(id); // Fire and forget
            return NoContent();
        }

        /// <summary>
        /// POST /api/homeblocks/{id}/track-view - Blok görüntülenmesini kaydeder
        /// Blok viewport'a girdiğinde çağrılır (Intersection Observer)
        /// </summary>
        [HttpPost("{id:int}/track-view")]
        [AllowAnonymous]
        [ProducesResponseType(204)]
        public async Task<IActionResult> TrackView(int id)
        {
            _ = _homeBlockService.TrackBlockViewAsync(id); // Fire and forget
            return NoContent();
        }

        /// <summary>
        /// Slug'a göre tek blok getirir - Tümünü Gör sayfası için
        /// Tüm ürünler döner (MaxProductCount sınırı yok)
        /// </summary>
        /// <param name="slug">Blok slug'ı (örn: indirimli-urunler)</param>
        /// <returns>Blok detayı ve tüm ürünleri</returns>
        [HttpGet("{slug}")]
        [AllowAnonymous]
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
        [AllowAnonymous]
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
        [HttpGet("preview")]
        [AllowAnonymous]
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

        /// <summary>
        /// POST /api/homeblocks - Yeni blok oluşturur (Admin)
        /// TargetCategoryId doğrulaması içerir
        /// </summary>
        [HttpPost]
        [Authorize(Roles = Roles.AdminLike)]
        [ProducesResponseType(typeof(HomeProductBlockDto), 201)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> CreateBlock([FromBody] CreateHomeBlockDto dto)
        {
            var targetCatId = dto.TargetCategoryId ?? dto.CategoryId;
            var (isValid, errorMessage) = await _homeBlockService.ValidateTargetCategoryAsync(targetCatId);
            if (!isValid)
            {
                return BadRequest(new { error = errorMessage, message = errorMessage });
            }

            var block = await _homeBlockService.CreateBlockAsync(dto);
            return CreatedAtAction(nameof(GetBlockBySlug), new { slug = block.Slug }, block);
        }

        /// <summary>
        /// PUT /api/homeblocks/{id} - Bloğu günceller (Admin)
        /// TargetCategoryId doğrulaması içerir
        /// </summary>
        [HttpPut("{id:int}")]
        [Authorize(Roles = Roles.AdminLike)]
        [ProducesResponseType(typeof(HomeProductBlockDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> UpdateBlock(int id, [FromBody] UpdateHomeBlockDto dto)
        {
            if (id != dto.Id)
            {
                return BadRequest(new { message = "ID mismatch" });
            }

            var targetCatId = dto.TargetCategoryId ?? dto.CategoryId;
            var (isValid, errorMessage) = await _homeBlockService.ValidateTargetCategoryAsync(targetCatId);
            if (!isValid)
            {
                return BadRequest(new { error = errorMessage, message = errorMessage });
            }

            var block = await _homeBlockService.UpdateBlockAsync(id, dto);
            if (block == null)
            {
                return NotFound(new { message = $"Blok #{id} bulunamadı" });
            }

            return Ok(block);
        }
    }
}
