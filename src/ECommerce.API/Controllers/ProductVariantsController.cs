using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Core.Constants;
using ECommerce.Core.DTOs.Product;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ECommerce.API.Controllers
{
    /// <summary>
    /// Ürün varyantları yönetimi için controller.
    /// Her varyant benzersiz SKU ile satın alınabilir stoklu bir birimdir.
    /// Public: Müşteriler için varyant sorgulama
    /// Admin: CRUD operasyonları, stok yönetimi
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ProductVariantsController : ControllerBase
    {
        private readonly IProductVariantService _variantService;
        private readonly ILogger<ProductVariantsController> _logger;

        public ProductVariantsController(
            IProductVariantService variantService,
            ILogger<ProductVariantsController> logger)
        {
            _variantService = variantService;
            _logger = logger;
        }

        #region Public Endpoints

        /// <summary>
        /// Belirli bir ürünün varyantlarını getirir (müşteriler için)
        /// </summary>
        /// <param name="productId">Ürün ID</param>
        /// <returns>Ürüne ait varyant listesi</returns>
        [HttpGet("product/{productId}")]
        public async Task<IActionResult> GetVariantsByProduct(int productId)
        {
            // Parametre doğrulama
            if (productId <= 0)
                return BadRequest(new { message = "Geçersiz ürün ID" });

            var variants = await _variantService.GetByProductIdAsync(productId);
            return Ok(variants);
        }

        /// <summary>
        /// SKU ile varyant getirir
        /// </summary>
        /// <param name="sku">Varyant SKU kodu</param>
        /// <returns>Varyant detayları</returns>
        [HttpGet("sku/{sku}")]
        public async Task<IActionResult> GetBySku(string sku)
        {
            // SKU boş olamaz
            if (string.IsNullOrWhiteSpace(sku))
                return BadRequest(new { message = "SKU gereklidir" });

            var variant = await _variantService.GetBySkuAsync(sku);
            if (variant == null)
                return NotFound(new { message = $"Varyant bulunamadı: {sku}" });

            return Ok(variant);
        }

        /// <summary>
        /// Varyant ID ile getirir
        /// </summary>
        /// <param name="id">Varyant ID</param>
        /// <returns>Varyant detayları</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            if (id <= 0)
                return BadRequest(new { message = "Geçersiz varyant ID" });

            var variant = await _variantService.GetByIdAsync(id);
            if (variant == null)
                return NotFound(new { message = "Varyant bulunamadı" });

            return Ok(variant);
        }

        #endregion

        #region Admin Endpoints

        /// <summary>
        /// Bir ürünün tüm varyantlarını getirir (aktif + pasif)
        /// </summary>
        /// <param name="productId">Ürün ID</param>
        /// <returns>Tüm varyantlar</returns>
        [HttpGet("admin/product/{productId}")]
        [Authorize(Roles = Roles.AdminLike)]
        public async Task<IActionResult> GetAllVariantsByProductAdmin(int productId)
        {
            if (productId <= 0)
                return BadRequest(new { message = "Geçersiz ürün ID" });

            // GetByProductIdAsync tüm varyantları (aktif+pasif) döndürür
            var variants = await _variantService.GetByProductIdAsync(productId);
            return Ok(variants);
        }

        /// <summary>
        /// Yeni varyant oluşturur. ProductId body'de belirtilmelidir.
        /// </summary>
        /// <param name="request">Varyant oluşturma isteği</param>
        /// <returns>Oluşturulan varyant</returns>
        [HttpPost]
        [Authorize(Roles = Roles.AdminLike)]
        public async Task<IActionResult> Create([FromBody] VariantCreateRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (request.ProductId <= 0)
                return BadRequest(new { message = "Geçerli bir ProductId gereklidir" });

            try
            {
                var result = await _variantService.CreateAsync(request.ProductId, request.Variant);
                return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
            }
            catch (InvalidOperationException ex)
            {
                // SKU çakışması gibi iş kuralı ihlalleri
                _logger.LogWarning(ex, "Varyant oluşturma hatası: {Message}", ex.Message);
                return Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Varyant oluşturulurken beklenmeyen hata");
                return StatusCode(500, new { message = "Bir hata oluştu" });
            }
        }

        /// <summary>
        /// Bir ürün için toplu varyant oluşturur
        /// </summary>
        /// <param name="request">Toplu varyant oluşturma isteği</param>
        /// <returns>Oluşturulan varyantlar</returns>
        [HttpPost("bulk")]
        [Authorize(Roles = Roles.AdminLike)]
        public async Task<IActionResult> CreateBulk([FromBody] BulkVariantCreateRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (request.ProductId <= 0)
                return BadRequest(new { message = "Geçerli bir ProductId gereklidir" });

            if (request.Variants == null || request.Variants.Count == 0)
                return BadRequest(new { message = "En az bir varyant gereklidir" });

            try
            {
                var results = new List<ProductVariantDetailDto>();
                foreach (var dto in request.Variants)
                {
                    var result = await _variantService.CreateAsync(request.ProductId, dto);
                    results.Add(result);
                }

                return Ok(new
                {
                    message = $"{results.Count} varyant oluşturuldu",
                    variants = results
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Toplu varyant oluşturulurken hata oluştu");
                return StatusCode(500, new { message = "Bir hata oluştu" });
            }
        }

        /// <summary>
        /// Varyant günceller
        /// </summary>
        /// <param name="id">Varyant ID</param>
        /// <param name="dto">Güncelleme verileri</param>
        /// <returns>Güncellenmiş varyant</returns>
        [HttpPut("{id}")]
        [Authorize(Roles = Roles.AdminLike)]
        public async Task<IActionResult> Update(int id, [FromBody] ProductVariantUpdateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (id <= 0)
                return BadRequest(new { message = "Geçersiz varyant ID" });

            try
            {
                var result = await _variantService.UpdateAsync(id, dto);
                return Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Varyant bulunamadı" });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Varyant güncelleme hatası: {Message}", ex.Message);
                return Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Varyant güncellenirken hata oluştu");
                return StatusCode(500, new { message = "Bir hata oluştu" });
            }
        }

        /// <summary>
        /// Varyantı siler (soft delete)
        /// </summary>
        /// <param name="id">Varyant ID</param>
        /// <returns>Silme sonucu</returns>
        [HttpDelete("{id}")]
        [Authorize(Roles = Roles.AdminLike)]
        public async Task<IActionResult> Delete(int id)
        {
            if (id <= 0)
                return BadRequest(new { message = "Geçersiz varyant ID" });

            var success = await _variantService.DeleteAsync(id);
            if (!success)
                return NotFound(new { message = "Varyant bulunamadı" });

            return Ok(new { message = "Varyant silindi" });
        }

        #endregion

        #region Stock Management

        /// <summary>
        /// Stok miktarını günceller
        /// </summary>
        /// <param name="id">Varyant ID</param>
        /// <param name="request">Stok güncelleme isteği</param>
        /// <returns>Güncelleme sonucu</returns>
        [HttpPatch("{id}/stock")]
        [Authorize(Roles = Roles.AdminLike)]
        public async Task<IActionResult> UpdateStock(int id, [FromBody] StockUpdateRequest request)
        {
            if (id <= 0)
                return BadRequest(new { message = "Geçersiz varyant ID" });

            if (request.Quantity < 0)
                return BadRequest(new { message = "Stok miktarı negatif olamaz" });

            var success = await _variantService.UpdateStockAsync(id, request.Quantity);
            if (!success)
                return NotFound(new { message = "Varyant bulunamadı" });

            return Ok(new { message = "Stok güncellendi", quantity = request.Quantity });
        }

        /// <summary>
        /// Toplu stok günceller (SKU bazlı)
        /// </summary>
        /// <param name="skuStockMap">SKU -> Stok miktarı dictionary</param>
        /// <returns>Güncelleme sonucu</returns>
        [HttpPatch("stock/bulk")]
        [Authorize(Roles = Roles.AdminLike)]
        public async Task<IActionResult> BulkUpdateStock([FromBody] Dictionary<string, int> skuStockMap)
        {
            if (skuStockMap == null || skuStockMap.Count == 0)
                return BadRequest(new { message = "En az bir SKU-stok çifti gereklidir" });

            // Negatif stok kontrolü
            if (skuStockMap.Values.Any(v => v < 0))
                return BadRequest(new { message = "Stok miktarı negatif olamaz" });

            var updated = await _variantService.BulkUpdateStockAsync(skuStockMap);
            return Ok(new
            {
                message = $"{updated} varyant stok güncellendi",
                updatedCount = updated
            });
        }

        /// <summary>
        /// Düşük stoklu varyantları getirir
        /// </summary>
        /// <param name="threshold">Stok eşiği (varsayılan: 10)</param>
        /// <returns>Düşük stoklu varyant listesi</returns>
        [HttpGet("admin/low-stock")]
        [Authorize(Roles = Roles.AdminLike)]
        public async Task<IActionResult> GetLowStock([FromQuery] int threshold = 10)
        {
            if (threshold < 0)
                return BadRequest(new { message = "Eşik değeri negatif olamaz" });

            var variants = await _variantService.GetLowStockVariantsAsync(threshold);
            return Ok(variants);
        }

        /// <summary>
        /// Varyant istatistiklerini getirir
        /// </summary>
        /// <param name="feedSourceId">Opsiyonel feed kaynağı filtresi</param>
        /// <returns>Varyant istatistikleri</returns>
        [HttpGet("admin/statistics")]
        [Authorize(Roles = Roles.AdminLike)]
        public async Task<IActionResult> GetStatistics([FromQuery] int? feedSourceId = null)
        {
            var stats = await _variantService.GetStatisticsAsync(feedSourceId);
            return Ok(stats);
        }

        #endregion
    }

    #region Request DTOs

    /// <summary>
    /// Tekil varyant oluşturma isteği
    /// </summary>
    public class VariantCreateRequest
    {
        /// <summary>
        /// Varyantın ait olacağı ürün ID
        /// </summary>
        public int ProductId { get; set; }

        /// <summary>
        /// Varyant bilgileri
        /// </summary>
        public ProductVariantCreateDto Variant { get; set; } = null!;
    }

    /// <summary>
    /// Toplu varyant oluşturma isteği
    /// </summary>
    public class BulkVariantCreateRequest
    {
        /// <summary>
        /// Varyantların ait olacağı ürün ID
        /// </summary>
        public int ProductId { get; set; }

        /// <summary>
        /// Varyant listesi
        /// </summary>
        public List<ProductVariantCreateDto> Variants { get; set; } = new();
    }

    /// <summary>
    /// Stok güncelleme isteği
    /// </summary>
    public class StockUpdateRequest
    {
        /// <summary>
        /// Yeni stok miktarı
        /// </summary>
        public int Quantity { get; set; }
    }

    #endregion
}
