using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Core.DTOs.HomeBlock;
using ECommerce.Core.Constants;

namespace ECommerce.API.Controllers
{
    /// <summary>
    /// Admin Ana Sayfa Blok Yönetimi Controller
    /// ------------------------------------------------
    /// Ana sayfa ürün bloklarının CRUD işlemleri ve ürün yönetimi.
    /// Sadece Admin yetkisi gerektirir.
    /// 
    /// Endpoint'ler:
    /// GET    /api/admin/homeblocks - Tüm blokları listele
    /// GET    /api/admin/homeblocks/{id} - Blok detayı
    /// POST   /api/admin/homeblocks - Yeni blok oluştur
    /// PUT    /api/admin/homeblocks/{id} - Blok güncelle
    /// DELETE /api/admin/homeblocks/{id} - Blok sil
    /// PUT    /api/admin/homeblocks/reorder - Blok sıralamasını güncelle
    /// 
    /// Ürün Yönetimi:
    /// POST   /api/admin/homeblocks/{id}/products - Bloğa ürün ekle
    /// DELETE /api/admin/homeblocks/{id}/products/{productId} - Ürün çıkar
    /// PUT    /api/admin/homeblocks/{id}/products - Ürünleri güncelle
    /// PUT    /api/admin/homeblocks/{id}/products/set - Ürün listesini değiştir
    /// </summary>
    [ApiController]
    [Route("api/admin/homeblocks")]
    [IgnoreAntiforgeryToken]
    [Authorize(Roles = Roles.AdminLike)]
    public class AdminHomeBlocksController : ControllerBase
    {
        private readonly IHomeBlockService _homeBlockService;
        private readonly ILogger<AdminHomeBlocksController> _logger;

        public AdminHomeBlocksController(
            IHomeBlockService homeBlockService,
            ILogger<AdminHomeBlocksController> logger)
        {
            _homeBlockService = homeBlockService;
            _logger = logger;
        }

        #region Block CRUD

        /// <summary>
        /// Tüm blokları listeler (admin için)
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<HomeProductBlockDto>), 200)]
        public async Task<IActionResult> GetAllBlocks()
        {
            _logger.LogInformation("📋 Admin: Tüm bloklar isteniyor");
            
            var blocks = await _homeBlockService.GetAllBlocksAsync();
            
            return Ok(blocks);
        }

        /// <summary>
        /// ID'ye göre blok detayı getirir
        /// </summary>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(HomeProductBlockDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetBlockById(int id)
        {
            _logger.LogInformation("🔍 Admin: Blok detayı isteniyor: #{Id}", id);
            
            var block = await _homeBlockService.GetBlockByIdAsync(id);
            
            if (block == null)
            {
                return NotFound(new { message = $"Blok #{id} bulunamadı" });
            }
            
            return Ok(block);
        }

        /// <summary>
        /// Yeni blok oluşturur
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(HomeProductBlockDto), 201)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> CreateBlock([FromBody] CreateHomeBlockDto dto)
        {
            // Validasyon
            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                return BadRequest(new { message = "Blok adı zorunludur" });
            }

            // Blok tipi validasyonu
            var validTypes = new[] { "manual", "category", "discounted", "newest", "bestseller" };
            if (!validTypes.Contains(dto.BlockType.ToLower()))
            {
                return BadRequest(new { 
                    message = $"Geçersiz blok tipi: {dto.BlockType}",
                    validTypes = validTypes
                });
            }

            // Kategori bazlı blok için CategoryId zorunlu
            if (dto.BlockType.ToLower() == "category" && !dto.CategoryId.HasValue)
            {
                return BadRequest(new { message = "Kategori bazlı bloklar için CategoryId zorunludur" });
            }

            _logger.LogInformation("➕ Admin: Yeni blok oluşturuluyor: {Name}", dto.Name);
            
            var created = await _homeBlockService.CreateBlockAsync(dto);
            
            return CreatedAtAction(nameof(GetBlockById), new { id = created.Id }, created);
        }

        /// <summary>
        /// Mevcut bloğu günceller
        /// </summary>
        [HttpPut("{id:int}")]
        [ProducesResponseType(typeof(HomeProductBlockDto), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> UpdateBlock(int id, [FromBody] UpdateHomeBlockDto dto)
        {
            if (id != dto.Id)
            {
                return BadRequest(new { message = "URL'deki ID ile body'deki ID eşleşmiyor" });
            }

            // Validasyon
            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                return BadRequest(new { message = "Blok adı zorunludur" });
            }

            _logger.LogInformation("✏️ Admin: Blok güncelleniyor: #{Id}", id);
            
            var updated = await _homeBlockService.UpdateBlockAsync(id, dto);
            
            if (updated == null)
            {
                return NotFound(new { message = $"Blok #{id} bulunamadı" });
            }
            
            return Ok(updated);
        }

        /// <summary>
        /// Bloğu siler
        /// </summary>
        [HttpDelete("{id:int}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteBlock(int id)
        {
            _logger.LogInformation("🗑️ Admin: Blok siliniyor: #{Id}", id);
            
            var result = await _homeBlockService.DeleteBlockAsync(id);
            
            if (!result)
            {
                return NotFound(new { message = $"Blok #{id} bulunamadı" });
            }
            
            return NoContent();
        }

        /// <summary>
        /// Blok sıralamasını toplu günceller
        /// </summary>
        [HttpPut("reorder")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> ReorderBlocks([FromBody] List<BlockOrderDto> orders)
        {
            _logger.LogInformation("🔄 Admin: Blok sıralaması güncelleniyor");
            
            var orderTuples = orders.Select(o => (o.Id, o.DisplayOrder));
            await _homeBlockService.UpdateBlocksOrderAsync(orderTuples);
            
            return Ok(new { message = "Sıralama güncellendi" });
        }

        /// <summary>
        /// Bloğun aktif/pasif durumunu tersine çevirir
        /// </summary>
        [HttpPut("{id:int}/toggle-active")]
        [ProducesResponseType(typeof(HomeProductBlockDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> ToggleBlockActive(int id)
        {
            _logger.LogInformation("🔄 Admin: Blok durumu değiştiriliyor: #{Id}", id);

            var updated = await _homeBlockService.ToggleBlockActiveAsync(id);
            if (updated == null)
            {
                return NotFound(new { message = $"Blok #{id} bulunamadı" });
            }

            return Ok(updated);
        }

        #endregion

        #region Block Products (Ürün Yönetimi)

        /// <summary>
        /// Bloğa ürün ekler
        /// </summary>
        [HttpPost("{blockId:int}/products")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> AddProductToBlock(int blockId, [FromBody] AddProductRequest request)
        {
            if (request.ProductId <= 0)
            {
                return BadRequest(new { message = "Geçerli bir ürün ID'si gerekli" });
            }

            _logger.LogInformation("➕ Admin: Ürün bloğa ekleniyor: Block#{BlockId} - Product#{ProductId}", 
                blockId, request.ProductId);

            var dto = new AddProductToBlockDto
            {
                BlockId = blockId,
                ProductId = request.ProductId,
                DisplayOrder = request.DisplayOrder
            };

            var result = await _homeBlockService.AddProductToBlockAsync(dto);
            
            if (!result)
            {
                return BadRequest(new { message = "Ürün bloğa eklenemedi" });
            }
            
            return Ok(new { message = "Ürün bloğa eklendi" });
        }

        /// <summary>
        /// Bloğa birden fazla ürün ekler
        /// </summary>
        [HttpPost("{blockId:int}/products/batch")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> AddProductsToBlock(int blockId, [FromBody] List<int> productIds)
        {
            if (productIds == null || !productIds.Any())
            {
                return BadRequest(new { message = "En az bir ürün ID'si gerekli" });
            }

            _logger.LogInformation("➕ Admin: {Count} ürün bloğa ekleniyor: Block#{BlockId}", 
                productIds.Count, blockId);

            var result = await _homeBlockService.AddProductsToBlockAsync(blockId, productIds);
            
            if (!result)
            {
                return BadRequest(new { message = "Ürünler bloğa eklenemedi" });
            }
            
            return Ok(new { message = $"{productIds.Count} ürün bloğa eklendi" });
        }

        /// <summary>
        /// Bloktan ürün çıkarır
        /// </summary>
        [HttpDelete("{blockId:int}/products/{productId:int}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> RemoveProductFromBlock(int blockId, int productId)
        {
            _logger.LogInformation("➖ Admin: Ürün bloktan çıkarılıyor: Block#{BlockId} - Product#{ProductId}", 
                blockId, productId);

            var result = await _homeBlockService.RemoveProductFromBlockAsync(blockId, productId);
            
            if (!result)
            {
                return NotFound(new { message = "Ürün blokta bulunamadı" });
            }
            
            return Ok(new { message = "Ürün bloktan çıkarıldı" });
        }

        /// <summary>
        /// Bloktaki ürünleri günceller (sıralama, aktiflik)
        /// </summary>
        [HttpPut("{blockId:int}/products")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> UpdateBlockProducts(int blockId, [FromBody] List<BlockProductOrderDto> products)
        {
            _logger.LogInformation("✏️ Admin: Blok ürünleri güncelleniyor: Block#{BlockId}", blockId);

            var dto = new UpdateBlockProductsDto
            {
                BlockId = blockId,
                Products = products
            };

            var result = await _homeBlockService.UpdateBlockProductsAsync(dto);
            
            if (!result)
            {
                return BadRequest(new { message = "Ürünler güncellenemedi" });
            }
            
            return Ok(new { message = "Ürünler güncellendi" });
        }

        /// <summary>
        /// Bloktaki ürün listesini tamamen değiştirir
        /// Önce tüm ürünler silinir, sonra yeni liste eklenir
        /// </summary>
        [HttpPut("{blockId:int}/products/set")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> SetBlockProducts(int blockId, [FromBody] List<int> productIds)
        {
            _logger.LogInformation("🔄 Admin: Blok ürünleri yenileniyor: Block#{BlockId} - {Count} ürün", 
                blockId, productIds?.Count ?? 0);

            var result = await _homeBlockService.SetBlockProductsAsync(blockId, productIds ?? new List<int>());
            
            if (!result)
            {
                return BadRequest(new { message = "Ürün listesi güncellenemedi" });
            }
            
            return Ok(new { message = "Ürün listesi güncellendi" });
        }

        #endregion

        #region Utility Endpoints

        /// <summary>
        /// Slug müsait mi kontrol eder
        /// </summary>
        [HttpGet("check-slug")]
        [ProducesResponseType(typeof(object), 200)]
        public async Task<IActionResult> CheckSlugAvailability(
            [FromQuery] string slug, 
            [FromQuery] int? excludeBlockId = null)
        {
            var isAvailable = await _homeBlockService.IsSlugAvailableAsync(slug, excludeBlockId);
            
            return Ok(new { 
                slug = slug, 
                isAvailable = isAvailable 
            });
        }

        #endregion
    }

    #region Request DTOs

    /// <summary>
    /// Ürün ekleme isteği
    /// </summary>
    public class AddProductRequest
    {
        public int ProductId { get; set; }
        public int DisplayOrder { get; set; } = 0;
    }

    /// <summary>
    /// Blok sıralama isteği
    /// </summary>
    public class BlockOrderDto
    {
        public int Id { get; set; }
        public int DisplayOrder { get; set; }
    }

    #endregion
}
