using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Core.Constants;
using ECommerce.Core.DTOs.ProductOption;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ECommerce.API.Controllers
{
    /// <summary>
    /// Ürün seçenekleri (Renk, Beden, Hacim vb.) yönetimi için controller.
    /// ProductOption: Seçenek türü (örn: Renk, Beden)
    /// ProductOptionValue: Seçenek değeri (örn: Kırmızı, XL)
    /// </summary>
    [ApiController]
    [Route("api/product-options")]
    public class ProductOptionsController : ControllerBase
    {
        private readonly IProductOptionService _optionService;
        private readonly ILogger<ProductOptionsController> _logger;

        public ProductOptionsController(
            IProductOptionService optionService,
            ILogger<ProductOptionsController> logger)
        {
            _optionService = optionService;
            _logger = logger;
        }

        #region Option Management

        /// <summary>
        /// Tüm seçenek türlerini listeler
        /// </summary>
        /// <returns>Seçenek türleri listesi</returns>
        [HttpGet]
        public async Task<IActionResult> GetAllOptions()
        {
            var options = await _optionService.GetAllOptionsAsync();
            return Ok(options);
        }

        /// <summary>
        /// ID ile seçenek türü getirir
        /// </summary>
        /// <param name="id">Seçenek türü ID</param>
        /// <returns>Seçenek türü detayları</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetOptionById(int id)
        {
            if (id <= 0)
                return BadRequest(new { message = "Geçersiz seçenek ID" });

            var option = await _optionService.GetOptionByIdAsync(id);
            if (option == null)
                return NotFound(new { message = "Seçenek türü bulunamadı" });

            return Ok(option);
        }

        /// <summary>
        /// Yeni seçenek türü oluşturur veya mevcut olanı getirir
        /// GetOrCreate pattern - idempotent operasyon
        /// </summary>
        /// <param name="request">Seçenek oluşturma isteği</param>
        /// <returns>Oluşturulan veya mevcut seçenek</returns>
        [HttpPost]
        [Authorize(Roles = Roles.AdminLike)]
        public async Task<IActionResult> CreateOption([FromBody] OptionCreateRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Boş isim kontrolü
            if (string.IsNullOrWhiteSpace(request.Name))
                return BadRequest(new { message = "Seçenek adı gereklidir" });

            // Normalleştirme - baştaki/sondaki boşlukları temizle
            var normalizedName = request.Name.Trim();

            try
            {
                var option = await _optionService.GetOrCreateOptionAsync(normalizedName);
                return Ok(option);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Seçenek türü oluşturulurken hata: {Name}", request.Name);
                return StatusCode(500, new { message = "Bir hata oluştu" });
            }
        }

        /// <summary>
        /// Seçenek türünü günceller
        /// </summary>
        /// <param name="id">Seçenek türü ID</param>
        /// <param name="dto">Güncelleme verileri</param>
        /// <returns>Güncellenmiş seçenek türü</returns>
        [HttpPut("{id}")]
        [Authorize(Roles = Roles.AdminLike)]
        public async Task<IActionResult> UpdateOption(int id, [FromBody] ProductOptionCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (id <= 0)
                return BadRequest(new { message = "Geçersiz seçenek ID" });

            try
            {
                var option = await _optionService.UpdateOptionAsync(id, dto);
                return Ok(option);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Seçenek türü bulunamadı" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Seçenek türü güncellenirken hata: ID={Id}", id);
                return StatusCode(500, new { message = "Bir hata oluştu" });
            }
        }

        /// <summary>
        /// Seçenek türünü siler (kullanımda değilse)
        /// </summary>
        /// <param name="id">Seçenek türü ID</param>
        /// <returns>Silme sonucu</returns>
        [HttpDelete("{id}")]
        [Authorize(Roles = Roles.AdminLike)]
        public async Task<IActionResult> DeleteOption(int id)
        {
            if (id <= 0)
                return BadRequest(new { message = "Geçersiz seçenek ID" });

            try
            {
                var success = await _optionService.DeleteOptionAsync(id);
                if (!success)
                    return Conflict(new { message = "Seçenek türü kullanımda olduğu için silinemez" });

                return Ok(new { message = "Seçenek türü silindi" });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Seçenek türü bulunamadı" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Seçenek türü silinirken hata: ID={Id}", id);
                return StatusCode(500, new { message = "Bir hata oluştu" });
            }
        }

        #endregion

        #region Option Value Management

        /// <summary>
        /// Bir seçenek türünün tüm değerlerini listeler
        /// </summary>
        /// <param name="optionId">Seçenek türü ID</param>
        /// <returns>Seçenek değerleri listesi</returns>
        [HttpGet("{optionId}/values")]
        public async Task<IActionResult> GetValuesByOptionId(int optionId)
        {
            if (optionId <= 0)
                return BadRequest(new { message = "Geçersiz seçenek ID" });

            var values = await _optionService.GetValuesByOptionIdAsync(optionId);
            return Ok(values);
        }

        /// <summary>
        /// Seçenek türüne yeni değer ekler veya mevcut olanı getirir
        /// GetOrCreate pattern - idempotent operasyon
        /// </summary>
        /// <param name="optionId">Seçenek türü ID</param>
        /// <param name="request">Değer ekleme isteği</param>
        /// <returns>Oluşturulan veya mevcut değer</returns>
        [HttpPost("{optionId}/values")]
        [Authorize(Roles = Roles.AdminLike)]
        public async Task<IActionResult> AddValueToOption(int optionId, [FromBody] ValueCreateRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (optionId <= 0)
                return BadRequest(new { message = "Geçersiz seçenek ID" });

            if (string.IsNullOrWhiteSpace(request.Value))
                return BadRequest(new { message = "Değer gereklidir" });

            try
            {
                var value = await _optionService.GetOrCreateValueAsync(optionId, request.Value.Trim());
                return Ok(value);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Seçenek türü bulunamadı" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Değer eklenirken hata: OptionId={OptionId}, Value={Value}", optionId, request.Value);
                return StatusCode(500, new { message = "Bir hata oluştu" });
            }
        }

        /// <summary>
        /// Seçenek türüne toplu değer ekler
        /// </summary>
        /// <param name="optionId">Seçenek türü ID</param>
        /// <param name="request">Toplu değer ekleme isteği</param>
        /// <returns>Oluşturulan veya mevcut değerler</returns>
        [HttpPost("{optionId}/values/bulk")]
        [Authorize(Roles = Roles.AdminLike)]
        public async Task<IActionResult> AddValuesToOption(int optionId, [FromBody] BulkValueCreateRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (optionId <= 0)
                return BadRequest(new { message = "Geçersiz seçenek ID" });

            if (request.Values == null || request.Values.Count == 0)
                return BadRequest(new { message = "En az bir değer gereklidir" });

            try
            {
                // Boş değerleri filtrele ve normalleştir
                var normalizedValues = request.Values
                    .Where(v => !string.IsNullOrWhiteSpace(v))
                    .Select(v => v.Trim())
                    .Distinct()
                    .ToList();

                if (normalizedValues.Count == 0)
                    return BadRequest(new { message = "Geçerli değer bulunamadı" });

                var values = await _optionService.GetOrCreateValuesAsync(optionId, normalizedValues);
                return Ok(new
                {
                    message = $"{normalizedValues.Count} değer işlendi",
                    values = values
                });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Seçenek türü bulunamadı" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Toplu değer eklenirken hata: OptionId={OptionId}", optionId);
                return StatusCode(500, new { message = "Bir hata oluştu" });
            }
        }

        /// <summary>
        /// Seçenek değerini günceller
        /// </summary>
        /// <param name="valueId">Değer ID</param>
        /// <param name="request">Güncelleme isteği</param>
        /// <returns>Güncellenmiş değer</returns>
        [HttpPut("values/{valueId}")]
        [Authorize(Roles = Roles.AdminLike)]
        public async Task<IActionResult> UpdateValue(int valueId, [FromBody] ValueUpdateRequest request)
        {
            if (valueId <= 0)
                return BadRequest(new { message = "Geçersiz değer ID" });

            if (string.IsNullOrWhiteSpace(request.NewValue))
                return BadRequest(new { message = "Yeni değer gereklidir" });

            try
            {
                var value = await _optionService.UpdateValueAsync(valueId, request.NewValue.Trim());
                return Ok(value);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Değer bulunamadı" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Değer güncellenirken hata: ValueId={ValueId}", valueId);
                return StatusCode(500, new { message = "Bir hata oluştu" });
            }
        }

        /// <summary>
        /// Seçenek değerini siler (kullanımda değilse)
        /// </summary>
        /// <param name="valueId">Değer ID</param>
        /// <returns>Silme sonucu</returns>
        [HttpDelete("values/{valueId}")]
        [Authorize(Roles = Roles.AdminLike)]
        public async Task<IActionResult> DeleteValue(int valueId)
        {
            if (valueId <= 0)
                return BadRequest(new { message = "Geçersiz değer ID" });

            try
            {
                var success = await _optionService.DeleteValueAsync(valueId);
                if (!success)
                    return Conflict(new { message = "Değer kullanımda olduğu için silinemez" });

                return Ok(new { message = "Değer silindi" });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Değer bulunamadı" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Değer silinirken hata: ValueId={ValueId}", valueId);
                return StatusCode(500, new { message = "Bir hata oluştu" });
            }
        }

        #endregion

        #region Product-Specific Operations

        /// <summary>
        /// Bir ürün için kullanılan seçenek türlerini listeler
        /// </summary>
        /// <param name="productId">Ürün ID</param>
        /// <returns>Ürünün seçenek türleri</returns>
        [HttpGet("by-product/{productId}")]
        public async Task<IActionResult> GetOptionsForProduct(int productId)
        {
            if (productId <= 0)
                return BadRequest(new { message = "Geçersiz ürün ID" });

            var options = await _optionService.GetOptionsForProductAsync(productId);
            return Ok(options);
        }

        /// <summary>
        /// Bir kategori için önerilen seçenek türlerini listeler
        /// </summary>
        /// <param name="categoryId">Kategori ID</param>
        /// <returns>Kategorideki popüler seçenekler</returns>
        [HttpGet("by-category/{categoryId}")]
        public async Task<IActionResult> GetOptionsForCategory(int categoryId)
        {
            if (categoryId <= 0)
                return BadRequest(new { message = "Geçersiz kategori ID" });

            var options = await _optionService.GetOptionsForCategoryAsync(categoryId);
            return Ok(options);
        }

        /// <summary>
        /// En çok kullanılan seçenek türlerini listeler
        /// </summary>
        /// <param name="limit">Maksimum sonuç sayısı (varsayılan: 10)</param>
        /// <returns>Popüler seçenek türleri</returns>
        [HttpGet("popular")]
        public async Task<IActionResult> GetMostUsedOptions([FromQuery] int limit = 10)
        {
            if (limit <= 0 || limit > 100)
                limit = 10;

            var options = await _optionService.GetMostUsedOptionsAsync(limit);
            return Ok(options);
        }

        #endregion
    }

    #region Request DTOs

    /// <summary>
    /// Seçenek türü oluşturma isteği
    /// </summary>
    public class OptionCreateRequest
    {
        /// <summary>
        /// Seçenek adı (örn: Renk, Beden, Hacim)
        /// </summary>
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// Seçenek değeri oluşturma isteği
    /// </summary>
    public class ValueCreateRequest
    {
        /// <summary>
        /// Değer (örn: Kırmızı, XL, 500ml)
        /// </summary>
        public string Value { get; set; } = string.Empty;
    }

    /// <summary>
    /// Toplu değer oluşturma isteği
    /// </summary>
    public class BulkValueCreateRequest
    {
        /// <summary>
        /// Değerler listesi
        /// </summary>
        public List<string> Values { get; set; } = new();
    }

    /// <summary>
    /// Değer güncelleme isteği
    /// </summary>
    public class ValueUpdateRequest
    {
        /// <summary>
        /// Yeni değer
        /// </summary>
        public string NewValue { get; set; } = string.Empty;
    }

    #endregion
}
