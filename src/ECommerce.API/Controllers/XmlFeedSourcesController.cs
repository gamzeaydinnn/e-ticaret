using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Core.Constants;
using ECommerce.Core.DTOs.XmlImport;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ECommerce.API.Controllers
{
    /// <summary>
    /// XML Feed kaynaklarını ve import işlemlerini yöneten controller.
    /// Tedarikçi XML beslemelerinden ürün/varyant aktarımı sağlar.
    /// 
    /// Endpoint Yapısı:
    /// - /api/xml-feeds: Feed kaynağı CRUD
    /// - /api/xml-feeds/{id}/sync: Manuel senkronizasyon
    /// - /api/xml-feeds/import: Import işlemleri
    /// </summary>
    [ApiController]
    [Route("api/xml-feeds")]
    [Authorize(Roles = Roles.AdminLike)]
    public class XmlFeedSourcesController : ControllerBase
    {
        private readonly IXmlFeedSourceService _feedSourceService;
        private readonly IXmlImportService _importService;
        private readonly ILogger<XmlFeedSourcesController> _logger;

        // Maksimum XML dosya boyutu: 50MB
        private const long MaxXmlFileSize = 50 * 1024 * 1024;

        public XmlFeedSourcesController(
            IXmlFeedSourceService feedSourceService,
            IXmlImportService importService,
            ILogger<XmlFeedSourcesController> logger)
        {
            _feedSourceService = feedSourceService;
            _importService = importService;
            _logger = logger;
        }

        #region Feed Source CRUD

        /// <summary>
        /// Tüm feed kaynaklarını listeler
        /// </summary>
        /// <returns>Feed kaynakları listesi</returns>
        [HttpGet]
        public async Task<IActionResult> GetAllFeedSources()
        {
            var sources = await _feedSourceService.GetAllAsync();
            return Ok(sources);
        }

        /// <summary>
        /// Aktif feed kaynaklarını listeler
        /// </summary>
        /// <returns>Aktif feed kaynakları</returns>
        [HttpGet("active")]
        public async Task<IActionResult> GetActiveFeedSources()
        {
            var sources = await _feedSourceService.GetActiveAsync();
            return Ok(sources);
        }

        /// <summary>
        /// ID ile feed kaynağı getirir
        /// </summary>
        /// <param name="id">Feed kaynağı ID</param>
        /// <returns>Feed kaynağı detayları</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetFeedSourceById(int id)
        {
            if (id <= 0)
                return BadRequest(new { message = "Geçersiz feed ID" });

            var source = await _feedSourceService.GetByIdAsync(id);
            if (source == null)
                return NotFound(new { message = "Feed kaynağı bulunamadı" });

            return Ok(source);
        }

        /// <summary>
        /// Yeni feed kaynağı oluşturur
        /// </summary>
        /// <param name="dto">Feed kaynağı bilgileri</param>
        /// <returns>Oluşturulan feed kaynağı</returns>
        [HttpPost]
        public async Task<IActionResult> CreateFeedSource([FromBody] XmlFeedSourceCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // URL doğrulama
            if (string.IsNullOrWhiteSpace(dto.Url))
                return BadRequest(new { message = "Feed URL gereklidir" });

            if (!Uri.TryCreate(dto.Url, UriKind.Absolute, out var uri))
                return BadRequest(new { message = "Geçersiz URL formatı" });

            try
            {
                var source = await _feedSourceService.CreateAsync(dto);
                return CreatedAtAction(nameof(GetFeedSourceById), new { id = source.Id }, source);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Feed kaynağı oluşturma hatası: {Message}", ex.Message);
                return Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Feed kaynağı oluşturulurken hata");
                return StatusCode(500, new { message = "Bir hata oluştu" });
            }
        }

        /// <summary>
        /// Feed kaynağını günceller
        /// </summary>
        /// <param name="id">Feed kaynağı ID</param>
        /// <param name="dto">Güncelleme bilgileri</param>
        /// <returns>Güncellenmiş feed kaynağı</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateFeedSource(int id, [FromBody] XmlFeedSourceCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (id <= 0)
                return BadRequest(new { message = "Geçersiz feed ID" });

            try
            {
                var source = await _feedSourceService.UpdateAsync(id, dto);
                return Ok(source);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Feed kaynağı bulunamadı" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Feed kaynağı güncellenirken hata: ID={Id}", id);
                return StatusCode(500, new { message = "Bir hata oluştu" });
            }
        }

        /// <summary>
        /// Feed kaynağını siler
        /// </summary>
        /// <param name="id">Feed kaynağı ID</param>
        /// <returns>Silme sonucu</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFeedSource(int id)
        {
            if (id <= 0)
                return BadRequest(new { message = "Geçersiz feed ID" });

            try
            {
                var success = await _feedSourceService.DeleteAsync(id);
                if (!success)
                    return NotFound(new { message = "Feed kaynağı bulunamadı" });

                return Ok(new { message = "Feed kaynağı silindi" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Feed kaynağı silinirken hata: ID={Id}", id);
                return StatusCode(500, new { message = "Bir hata oluştu" });
            }
        }

        #endregion

        #region Feed Status & Activation

        /// <summary>
        /// Feed kaynağının durumunu getirir
        /// </summary>
        /// <param name="id">Feed kaynağı ID</param>
        /// <returns>Feed durumu</returns>
        [HttpGet("{id}/status")]
        public async Task<IActionResult> GetFeedStatus(int id)
        {
            if (id <= 0)
                return BadRequest(new { message = "Geçersiz feed ID" });

            var status = await _feedSourceService.GetStatusAsync(id);
            if (status == null)
                return NotFound(new { message = "Feed kaynağı bulunamadı" });

            return Ok(status);
        }

        /// <summary>
        /// Feed kaynağını aktif/pasif yapar
        /// </summary>
        /// <param name="id">Feed kaynağı ID</param>
        /// <param name="request">Aktivasyon isteği</param>
        /// <returns>İşlem sonucu</returns>
        [HttpPatch("{id}/active")]
        public async Task<IActionResult> SetFeedActiveStatus(int id, [FromBody] SetActiveRequest request)
        {
            if (id <= 0)
                return BadRequest(new { message = "Geçersiz feed ID" });

            try
            {
                var success = await _feedSourceService.SetActiveStatusAsync(id, request.IsActive);
                if (!success)
                    return NotFound(new { message = "Feed kaynağı bulunamadı" });

                var statusText = request.IsActive ? "aktif" : "pasif";
                return Ok(new { message = $"Feed kaynağı {statusText} yapıldı" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Feed aktivasyon hatası: ID={Id}", id);
                return StatusCode(500, new { message = "Bir hata oluştu" });
            }
        }

        #endregion

        #region Mapping Configuration

        /// <summary>
        /// Feed kaynağının mapping konfigürasyonunu getirir
        /// </summary>
        /// <param name="id">Feed kaynağı ID</param>
        /// <returns>Mapping konfigürasyonu</returns>
        [HttpGet("{id}/mapping")]
        public async Task<IActionResult> GetMappingConfig(int id)
        {
            if (id <= 0)
                return BadRequest(new { message = "Geçersiz feed ID" });

            var config = await _feedSourceService.GetMappingConfigAsync(id);
            if (config == null)
                return NotFound(new { message = "Feed kaynağı bulunamadı" });

            return Ok(config);
        }

        /// <summary>
        /// Feed kaynağının mapping konfigürasyonunu günceller
        /// </summary>
        /// <param name="id">Feed kaynağı ID</param>
        /// <param name="config">Yeni mapping konfigürasyonu</param>
        /// <returns>Güncelleme sonucu</returns>
        [HttpPut("{id}/mapping")]
        public async Task<IActionResult> UpdateMappingConfig(int id, [FromBody] XmlMappingConfigDto config)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (id <= 0)
                return BadRequest(new { message = "Geçersiz feed ID" });

            try
            {
                var success = await _feedSourceService.UpdateMappingConfigAsync(id, config);
                if (!success)
                    return NotFound(new { message = "Feed kaynağı bulunamadı" });

                return Ok(new { message = "Mapping konfigürasyonu güncellendi" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Mapping güncelleme hatası: ID={Id}", id);
                return StatusCode(500, new { message = "Bir hata oluştu" });
            }
        }

        /// <summary>
        /// Feed içeriğinden otomatik mapping önerir
        /// </summary>
        /// <param name="id">Feed kaynağı ID</param>
        /// <returns>Önerilen mapping konfigürasyonu</returns>
        [HttpGet("{id}/suggest-mapping")]
        public async Task<IActionResult> SuggestMapping(int id)
        {
            if (id <= 0)
                return BadRequest(new { message = "Geçersiz feed ID" });

            try
            {
                var suggestedConfig = await _feedSourceService.SuggestMappingAsync(id);
                return Ok(suggestedConfig);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Feed kaynağı bulunamadı" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Mapping önerisi hatası: ID={Id}", id);
                return StatusCode(500, new { message = "Bir hata oluştu" });
            }
        }

        #endregion

        #region Sync & Import

        /// <summary>
        /// Feed kaynağını manuel senkronize eder
        /// </summary>
        /// <param name="id">Feed kaynağı ID</param>
        /// <returns>Import sonucu</returns>
        [HttpPost("{id}/sync")]
        public async Task<IActionResult> SyncFeed(int id)
        {
            if (id <= 0)
                return BadRequest(new { message = "Geçersiz feed ID" });

            try
            {
                _logger.LogInformation("Manuel senkronizasyon başlatıldı: FeedId={Id}", id);
                var result = await _importService.ForceSyncFeedAsync(id);
                return Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Feed kaynağı bulunamadı" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Senkronizasyon hatası: FeedId={Id}", id);
                return StatusCode(500, new { message = "Senkronizasyon sırasında hata oluştu" });
            }
        }

        /// <summary>
        /// URL'den doğrudan import yapar (tek seferlik)
        /// </summary>
        /// <param name="request">Import isteği</param>
        /// <returns>Import sonucu</returns>
        [HttpPost("import/url")]
        public async Task<IActionResult> ImportFromUrl([FromBody] UrlImportRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (string.IsNullOrWhiteSpace(request.Url))
                return BadRequest(new { message = "URL gereklidir" });

            if (!Uri.TryCreate(request.Url, UriKind.Absolute, out _))
                return BadRequest(new { message = "Geçersiz URL formatı" });

            if (request.Mapping == null)
                return BadRequest(new { message = "Mapping konfigürasyonu gereklidir" });

            try
            {
                _logger.LogInformation("URL'den import başlatıldı: {Url}", request.Url);
                var result = await _importService.ImportFromUrlAsync(request.Url, request.Mapping);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "URL import hatası: {Url}", request.Url);
                return StatusCode(500, new { message = "Import sırasında hata oluştu" });
            }
        }

        /// <summary>
        /// Dosya yükleyerek import yapar
        /// </summary>
        /// <param name="file">XML dosyası</param>
        /// <param name="mappingJson">Mapping konfigürasyonu (JSON string)</param>
        /// <param name="feedSourceId">Opsiyonel feed kaynağı ID</param>
        /// <returns>Import sonucu</returns>
        [HttpPost("import/file")]
        [RequestSizeLimit(MaxXmlFileSize)]
        public async Task<IActionResult> ImportFromFile(
            IFormFile file,
            [FromForm] string mappingJson,
            [FromForm] int? feedSourceId = null)
        {
            // Dosya kontrolü
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "XML dosyası gereklidir" });

            if (file.Length > MaxXmlFileSize)
                return BadRequest(new { message = $"Dosya boyutu çok büyük. Maksimum: {MaxXmlFileSize / (1024 * 1024)}MB" });

            // Dosya tipi kontrolü
            var allowedExtensions = new[] { ".xml" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
                return BadRequest(new { message = "Sadece XML dosyaları kabul edilir" });

            // Mapping parse
            XmlMappingConfigDto? mapping;
            try
            {
                mapping = System.Text.Json.JsonSerializer.Deserialize<XmlMappingConfigDto>(mappingJson);
                if (mapping == null)
                    return BadRequest(new { message = "Geçersiz mapping konfigürasyonu" });
            }
            catch (System.Text.Json.JsonException)
            {
                return BadRequest(new { message = "Mapping JSON formatı geçersiz" });
            }

            try
            {
                // Dosya içeriğini oku
                using var reader = new StreamReader(file.OpenReadStream(), Encoding.UTF8);
                var xmlContent = await reader.ReadToEndAsync();

                _logger.LogInformation("Dosyadan import başlatıldı: {FileName}, Boyut={Size}KB",
                    file.FileName, file.Length / 1024);

                var result = await _importService.ImportFromContentAsync(xmlContent, mapping, feedSourceId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Dosya import hatası: {FileName}", file.FileName);
                return StatusCode(500, new { message = "Import sırasında hata oluştu" });
            }
        }

        #endregion

        #region Preview & Validation

        /// <summary>
        /// Feed içeriğini önizler
        /// </summary>
        /// <param name="id">Feed kaynağı ID</param>
        /// <param name="sampleSize">Örnek ürün sayısı (varsayılan: 5)</param>
        /// <returns>Önizleme sonucu</returns>
        [HttpGet("{id}/preview")]
        public async Task<IActionResult> PreviewFeed(int id, [FromQuery] int sampleSize = 5)
        {
            if (id <= 0)
                return BadRequest(new { message = "Geçersiz feed ID" });

            if (sampleSize <= 0 || sampleSize > 50)
                sampleSize = 5;

            try
            {
                var preview = await _feedSourceService.PreviewFeedAsync(id, sampleSize);
                return Ok(preview);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Feed kaynağı bulunamadı" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Feed önizleme hatası: ID={Id}", id);
                return StatusCode(500, new { message = "Önizleme sırasında hata oluştu" });
            }
        }

        /// <summary>
        /// Feed URL'sini doğrular
        /// </summary>
        /// <param name="request">URL doğrulama isteği</param>
        /// <returns>Doğrulama sonucu</returns>
        [HttpPost("validate-url")]
        public async Task<IActionResult> ValidateFeedUrl([FromBody] ValidateUrlRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Url))
                return BadRequest(new { message = "URL gereklidir" });

            if (!Uri.TryCreate(request.Url, UriKind.Absolute, out _))
                return BadRequest(new { message = "Geçersiz URL formatı" });

            try
            {
                var result = await _feedSourceService.ValidateFeedUrlAsync(request.Url);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "URL doğrulama hatası: {Url}", request.Url);
                return StatusCode(500, new { message = "Doğrulama sırasında hata oluştu" });
            }
        }

        /// <summary>
        /// Import önizlemesi yapar (veritabanını değiştirmez)
        /// </summary>
        /// <param name="id">Feed kaynağı ID</param>
        /// <param name="sampleSize">Örnek ürün sayısı (varsayılan: 10)</param>
        /// <returns>Import önizlemesi</returns>
        [HttpGet("{id}/import-preview")]
        public async Task<IActionResult> PreviewImport(int id, [FromQuery] int sampleSize = 10)
        {
            if (id <= 0)
                return BadRequest(new { message = "Geçersiz feed ID" });

            if (sampleSize <= 0 || sampleSize > 100)
                sampleSize = 10;

            try
            {
                var preview = await _importService.PreviewImportAsync(id, sampleSize);
                return Ok(preview);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Feed kaynağı bulunamadı" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Import önizleme hatası: ID={Id}", id);
                return StatusCode(500, new { message = "Önizleme sırasında hata oluştu" });
            }
        }

        #endregion

        #region Import Progress & History

        /// <summary>
        /// Aktif import işlemlerini listeler
        /// </summary>
        /// <returns>Aktif import listesi</returns>
        [HttpGet("imports/active")]
        public async Task<IActionResult> GetActiveImports()
        {
            var imports = await _importService.GetActiveImportsAsync();
            return Ok(imports);
        }

        /// <summary>
        /// Import işleminin durumunu getirir
        /// </summary>
        /// <param name="importId">Import ID</param>
        /// <returns>Import durumu</returns>
        [HttpGet("imports/{importId}")]
        public async Task<IActionResult> GetImportProgress(string importId)
        {
            if (string.IsNullOrWhiteSpace(importId))
                return BadRequest(new { message = "Import ID gereklidir" });

            var progress = await _importService.GetImportProgressAsync(importId);
            if (progress == null)
                return NotFound(new { message = "Import işlemi bulunamadı" });

            return Ok(progress);
        }

        /// <summary>
        /// Import işlemini iptal eder
        /// </summary>
        /// <param name="importId">Import ID</param>
        /// <returns>İptal sonucu</returns>
        [HttpPost("imports/{importId}/cancel")]
        public async Task<IActionResult> CancelImport(string importId)
        {
            if (string.IsNullOrWhiteSpace(importId))
                return BadRequest(new { message = "Import ID gereklidir" });

            try
            {
                var success = await _importService.CancelImportAsync(importId);
                if (!success)
                    return NotFound(new { message = "İptal edilecek aktif import bulunamadı" });

                return Ok(new { message = "Import iptal edildi" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Import iptal hatası: {ImportId}", importId);
                return StatusCode(500, new { message = "İptal sırasında hata oluştu" });
            }
        }

        /// <summary>
        /// Import geçmişini listeler
        /// </summary>
        /// <param name="feedSourceId">Opsiyonel feed kaynağı filtresi</param>
        /// <param name="page">Sayfa numarası (varsayılan: 1)</param>
        /// <param name="pageSize">Sayfa boyutu (varsayılan: 20)</param>
        /// <returns>Import geçmişi</returns>
        [HttpGet("imports/history")]
        public async Task<IActionResult> GetImportHistory(
            [FromQuery] int? feedSourceId = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0 || pageSize > 100) pageSize = 20;

            var history = await _importService.GetImportHistoryAsync(feedSourceId, page, pageSize);
            return Ok(history);
        }

        #endregion

        #region Cleanup

        /// <summary>
        /// Feed'den kaldırılmış (stale) ürünleri deaktif eder
        /// </summary>
        /// <param name="id">Feed kaynağı ID</param>
        /// <param name="hoursThreshold">Eşik süresi (saat, varsayılan: 48)</param>
        /// <returns>Temizleme sonucu</returns>
        [HttpPost("{id}/cleanup")]
        public async Task<IActionResult> CleanupStaleProducts(int id, [FromQuery] int hoursThreshold = 48)
        {
            if (id <= 0)
                return BadRequest(new { message = "Geçersiz feed ID" });

            if (hoursThreshold <= 0)
                return BadRequest(new { message = "Eşik süresi pozitif olmalıdır" });

            try
            {
                var cleanedCount = await _importService.CleanupStaleProductsAsync(id, hoursThreshold);
                return Ok(new
                {
                    message = $"{cleanedCount} ürün deaktif edildi",
                    cleanedCount = cleanedCount
                });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Feed kaynağı bulunamadı" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cleanup hatası: FeedId={Id}", id);
                return StatusCode(500, new { message = "Temizleme sırasında hata oluştu" });
            }
        }

        #endregion
    }

    #region Request DTOs

    /// <summary>
    /// Aktivasyon isteği
    /// </summary>
    public class SetActiveRequest
    {
        /// <summary>
        /// Aktif durumu
        /// </summary>
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// URL'den import isteği
    /// </summary>
    public class UrlImportRequest
    {
        /// <summary>
        /// XML feed URL'si
        /// </summary>
        public string Url { get; set; } = string.Empty;

        /// <summary>
        /// Mapping konfigürasyonu
        /// </summary>
        public XmlMappingConfigDto? Mapping { get; set; }
    }

    /// <summary>
    /// URL doğrulama isteği
    /// </summary>
    public class ValidateUrlRequest
    {
        /// <summary>
        /// Doğrulanacak URL
        /// </summary>
        public string Url { get; set; } = string.Empty;
    }

    #endregion
}
