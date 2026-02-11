using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Core.Constants;
using ECommerce.Core.DTOs.Promotions;
using ECommerce.Core.Interfaces;
using ECommerce.Entities.Concrete;
using ECommerce.Entities.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ECommerce.API.Authorization;

namespace ECommerce.API.Controllers.Admin
{
    /// <summary>
    /// Admin kampanya yönetimi controller'ı.
    /// Kampanya CRUD operasyonları, hedef yönetimi ve durum değişikliklerini sağlar.
    /// 
    /// Desteklenen kampanya türleri:
    /// - Percentage: Yüzdelik indirim (örn: %10)
    /// - FixedAmount: Sabit tutar indirim (örn: 50 TL)
    /// - BuyXPayY: X al Y öde (örn: 3 al 2 öde)
    /// - FreeShipping: Ücretsiz kargo
    /// </summary>
    [Authorize(Roles = Roles.AllStaff)]
    [ApiController]
    [Route("api/admin/campaigns")]
    public class AdminCampaignsController : ControllerBase
    {
        private readonly ICampaignService _campaignService;
        private readonly ICampaignApplicationService _campaignApplicationService;
        private readonly IProductRepository _productRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IFileStorage _fileStorage;
        private readonly ILogger<AdminCampaignsController> _logger;

        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        private static readonly string[] AllowedMimeTypes = { "image/jpeg", "image/png", "image/gif", "image/webp" };
        private const long MaxFileSize = 10 * 1024 * 1024;

        public AdminCampaignsController(
            ICampaignService campaignService,
            ICampaignApplicationService campaignApplicationService,
            IProductRepository productRepository,
            ICategoryRepository categoryRepository,
            IFileStorage fileStorage,
            ILogger<AdminCampaignsController> logger)
        {
            _campaignService = campaignService ?? throw new ArgumentNullException(nameof(campaignService));
            _campaignApplicationService = campaignApplicationService ?? throw new ArgumentNullException(nameof(campaignApplicationService));
            _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
            _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
            _fileStorage = fileStorage ?? throw new ArgumentNullException(nameof(fileStorage));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region CRUD Operasyonları

        /// <summary>
        /// Tüm kampanyaları listeler.
        /// </summary>
        [HttpGet]
        [HasPermission(Permissions.Campaigns.View)]
        public async Task<ActionResult<IEnumerable<CampaignListDto>>> GetCampaigns()
        {
            try
            {
                var campaigns = await _campaignService.GetAllAsync();
                var result = campaigns.Select(MapToListDto).ToList();
                
                _logger.LogDebug("Kampanya listesi getirildi. Toplam: {Count}", result.Count);
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kampanya listesi getirme hatası");
                return StatusCode(500, new { message = "Kampanyalar yüklenirken hata oluştu." });
            }
        }

        /// <summary>
        /// Belirli bir kampanyanın detaylarını getirir.
        /// </summary>
        [HttpGet("{id:int}")]
        [HasPermission(Permissions.Campaigns.View)]
        public async Task<ActionResult<CampaignDetailDto>> GetCampaignById(int id)
        {
            try
            {
                var campaign = await _campaignService.GetByIdAsync(id);
                if (campaign == null)
                {
                    return NotFound(new { message = "Kampanya bulunamadı." });
                }

                var dto = await MapToDetailDtoAsync(campaign);
                return Ok(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kampanya detay getirme hatası. ID: {CampaignId}", id);
                return StatusCode(500, new { message = "Kampanya detayları yüklenirken hata oluştu." });
            }
        }

        /// <summary>
        /// Yeni kampanya oluşturur.
        /// </summary>
        [HttpPost]
        [HasPermission(Permissions.Campaigns.Create)]
        public async Task<ActionResult<CampaignDetailDto>> CreateCampaign([FromBody] CampaignSaveDto dto)
        {
            try
            {
                // Validasyon
                var validationResult = await ValidateCampaignDto(dto);
                if (validationResult != null)
                {
                    return validationResult;
                }

                var created = await _campaignService.CreateAsync(dto);
                
                // Kampanya Uygulama Motoru: Ürünlere otomatik indirim uygula
                var updatedProductCount = await _campaignApplicationService.ApplyCampaignToProductsAsync(created.Id);
                _logger.LogInformation(
                    "Kampanya ürünlere uygulandı. Kampanya ID: {CampaignId}, Güncellenen ürün: {UpdatedCount}",
                    created.Id, updatedProductCount);
                
                var detailDto = await MapToDetailDtoAsync(created);
                
                _logger.LogInformation(
                    "Kampanya oluşturuldu. ID: {CampaignId}, Ad: {Name}, Tür: {Type}",
                    created.Id, created.Name, created.Type);
                
                return CreatedAtAction(nameof(GetCampaignById), new { id = detailDto.Id }, detailDto);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Kampanya oluşturma validasyon hatası");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kampanya oluşturma hatası");
                return StatusCode(500, new { message = "Kampanya oluşturulurken hata oluştu." });
            }
        }

        /// <summary>
        /// Mevcut kampanyayı günceller.
        /// </summary>
        [HttpPut("{id:int}")]
        [HasPermission(Permissions.Campaigns.Update)]
        public async Task<ActionResult<CampaignDetailDto>> UpdateCampaign(int id, [FromBody] CampaignSaveDto dto)
        {
            try
            {
                // Validasyon
                var validationResult = await ValidateCampaignDto(dto);
                if (validationResult != null)
                {
                    return validationResult;
                }

                var existing = await _campaignService.GetByIdAsync(id);
                if (existing == null)
                {
                    return NotFound(new { message = "Kampanya bulunamadı." });
                }

                var updated = await _campaignService.UpdateAsync(id, dto);
                
                // Kampanya Uygulama Motoru: Ürünlere güncel indirimi uygula
                var updatedProductCount = await _campaignApplicationService.ApplyCampaignToProductsAsync(updated.Id);
                _logger.LogInformation(
                    "Kampanya güncellemesi ürünlere uygulandı. Kampanya ID: {CampaignId}, Güncellenen ürün: {UpdatedCount}",
                    updated.Id, updatedProductCount);
                
                var detailDto = await MapToDetailDtoAsync(updated);
                
                _logger.LogInformation(
                    "Kampanya güncellendi. ID: {CampaignId}, Ad: {Name}",
                    updated.Id, updated.Name);
                
                return Ok(detailDto);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Kampanya güncelleme validasyon hatası");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kampanya güncelleme hatası. ID: {CampaignId}", id);
                return StatusCode(500, new { message = "Kampanya güncellenirken hata oluştu." });
            }
        }

        /// <summary>
        /// Kampanyayı siler.
        /// </summary>
        [HttpDelete("{id:int}")]
        [HasPermission(Permissions.Campaigns.Delete)]
        public async Task<IActionResult> DeleteCampaign(int id)
        {
            try
            {
                // Önce kampanya indirimlerini ürünlerden kaldır
                var removedProductCount = await _campaignApplicationService.RemoveCampaignFromProductsAsync(id);
                _logger.LogInformation(
                    "Kampanya indirimleri ürünlerden kaldırıldı. Kampanya ID: {CampaignId}, Güncellenen ürün: {UpdatedCount}",
                    id, removedProductCount);
                
                var deleted = await _campaignService.DeleteAsync(id);
                if (!deleted)
                {
                    return NotFound(new { message = "Kampanya bulunamadı." });
                }

                _logger.LogInformation("Kampanya silindi. ID: {CampaignId}", id);
                
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kampanya silme hatası. ID: {CampaignId}", id);
                return StatusCode(500, new { message = "Kampanya silinirken hata oluştu." });
            }
        }

        /// <summary>
        /// Kampanya görseli yükler. Opsiyonel - kampanya görselsiz de çalışır.
        /// Güvenlik önlemleri:
        /// - Path traversal koruması (filename sanitization)
        /// - Magic number validation (gerçek dosya içeriği kontrolü)
        /// - MIME type ve extension double check
        /// - Dosya boyutu limiti
        /// </summary>
        [HttpPost("upload-image")]
        [HasPermission(Permissions.Campaigns.Create)]
        public async Task<IActionResult> UploadCampaignImage(IFormFile image)
        {
            try
            {
                // 1. Null ve boş dosya kontrolü
                if (image == null || image.Length == 0)
                    return BadRequest(new { message = "Lütfen bir resim dosyası seçin." });

                // 2. Dosya boyutu kontrolü (10MB)
                if (image.Length > MaxFileSize)
                    return BadRequest(new { message = $"Dosya boyutu maksimum {MaxFileSize / (1024 * 1024)}MB olabilir." });

                // 3. Extension kontrolü (client-side bypass koruması)
                var extension = Path.GetExtension(image.FileName).ToLowerInvariant();
                if (!AllowedExtensions.Contains(extension))
                    return BadRequest(new { message = $"Desteklenen dosya türleri: {string.Join(", ", AllowedExtensions)}" });

                // 4. MIME type kontrolü (client-side bypass koruması)
                var mimeType = image.ContentType.ToLowerInvariant();
                if (!AllowedMimeTypes.Contains(mimeType))
                    return BadRequest(new { message = "Geçersiz dosya türü. Sadece resim dosyaları kabul edilir." });

                string imageUrl;
                using (var stream = image.OpenReadStream())
                {
                    // 5. MAGIC NUMBER VALIDATION (en güvenli kontrol)
                    // Dosya içeriğinin gerçekten image olup olmadığını kontrol eder
                    // Extension veya MIME type değiştirme saldırılarını engeller
                    if (!IsValidImageFile(stream))
                    {
                        _logger.LogWarning(
                            "Geçersiz image file upload denemesi. FileName: {FileName}, ContentType: {ContentType}",
                            image.FileName, image.ContentType);

                        return BadRequest(new
                        {
                            message = "Dosya içeriği geçersiz. Sadece gerçek resim dosyaları yüklenebilir."
                        });
                    }

                    // 6. Güvenli ve benzersiz filename oluştur
                    // Path traversal saldırılarını engeller (../../etc/passwd gibi)
                    // GUID ile collision'ları önler
                    var safeFileName = SanitizeAndGenerateUniqueFileName(image.FileName);

                    _logger.LogInformation(
                        "Güvenli filename oluşturuldu. Original: {Original}, Safe: {Safe}",
                        image.FileName, safeFileName);

                    // 7. Dosyayı güvenli storage'a yükle
                    imageUrl = await _fileStorage.UploadAsync(stream, safeFileName, image.ContentType);
                }

                _logger.LogInformation(
                    "Kampanya görseli başarıyla yüklendi. URL: {ImageUrl}", imageUrl);

                return Ok(new { success = true, imageUrl });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kampanya görseli yüklenirken hata oluştu. FileName: {FileName}",
                    image?.FileName ?? "unknown");

                return StatusCode(500, new { message = "Görsel yüklenirken hata oluştu." });
            }
        }

        #endregion

        #region Durum Yönetimi

        /// <summary>
        /// Kampanya aktif/pasif durumunu değiştirir.
        /// </summary>
        [HttpPatch("{id:int}/toggle")]
        [HasPermission(Permissions.Campaigns.Update)]
        public async Task<ActionResult<object>> ToggleCampaignStatus(int id)
        {
            try
            {
                var result = await _campaignService.ToggleActiveAsync(id);
                if (!result)
                {
                    return NotFound(new { message = "Kampanya bulunamadı." });
                }

                // Güncel durumu getir
                var campaign = await _campaignService.GetByIdAsync(id);
                
                // Kampanya Uygulama Motoru: Duruma göre indirimleri uygula/kaldır
                if (campaign?.IsActive == true)
                {
                    var updatedCount = await _campaignApplicationService.ApplyCampaignToProductsAsync(id);
                    _logger.LogInformation(
                        "Kampanya aktif edildi, indirimler uygulandı. ID: {CampaignId}, Güncellenen ürün: {UpdatedCount}",
                        id, updatedCount);
                }
                else
                {
                    var updatedCount = await _campaignApplicationService.RemoveCampaignFromProductsAsync(id);
                    _logger.LogInformation(
                        "Kampanya pasif edildi, indirimler kaldırıldı. ID: {CampaignId}, Güncellenen ürün: {UpdatedCount}",
                        id, updatedCount);
                }
                
                _logger.LogInformation(
                    "Kampanya durumu değiştirildi. ID: {CampaignId}, Yeni Durum: {IsActive}",
                    id, campaign?.IsActive);
                
                return Ok(new 
                { 
                    success = true, 
                    isActive = campaign?.IsActive ?? false,
                    message = campaign?.IsActive == true ? "Kampanya aktif edildi." : "Kampanya pasif edildi."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kampanya durum değiştirme hatası. ID: {CampaignId}", id);
                return StatusCode(500, new { message = "Kampanya durumu değiştirilirken hata oluştu." });
            }
        }

        /// <summary>
        /// Kampanyayı aktif eder.
        /// </summary>
        [HttpPatch("{id:int}/activate")]
        [HasPermission(Permissions.Campaigns.Update)]
        public async Task<IActionResult> ActivateCampaign(int id)
        {
            try
            {
                var campaign = await _campaignService.GetByIdAsync(id);
                if (campaign == null)
                {
                    return NotFound(new { message = "Kampanya bulunamadı." });
                }

                if (!campaign.IsActive)
                {
                    await _campaignService.ToggleActiveAsync(id);
                    
                    // Kampanya Uygulama Motoru: İndirimleri uygula
                    var updatedCount = await _campaignApplicationService.ApplyCampaignToProductsAsync(id);
                    _logger.LogInformation(
                        "Kampanya aktif edildi, indirimler uygulandı. ID: {CampaignId}, Güncellenen ürün: {UpdatedCount}",
                        id, updatedCount);
                }
                
                return Ok(new { success = true, message = "Kampanya aktif edildi." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kampanya aktif etme hatası. ID: {CampaignId}", id);
                return StatusCode(500, new { message = "Kampanya aktif edilirken hata oluştu." });
            }
        }

        /// <summary>
        /// Kampanyayı pasif eder.
        /// </summary>
        [HttpPatch("{id:int}/deactivate")]
        [HasPermission(Permissions.Campaigns.Update)]
        public async Task<IActionResult> DeactivateCampaign(int id)
        {
            try
            {
                var campaign = await _campaignService.GetByIdAsync(id);
                if (campaign == null)
                {
                    return NotFound(new { message = "Kampanya bulunamadı." });
                }

                if (campaign.IsActive)
                {
                    // Önce indirimleri kaldır
                    var updatedCount = await _campaignApplicationService.RemoveCampaignFromProductsAsync(id);
                    _logger.LogInformation(
                        "Kampanya pasif edildi, indirimler kaldırıldı. ID: {CampaignId}, Güncellenen ürün: {UpdatedCount}",
                        id, updatedCount);
                    
                    await _campaignService.ToggleActiveAsync(id);
                }
                
                return Ok(new { success = true, message = "Kampanya pasif edildi." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kampanya pasif etme hatası. ID: {CampaignId}", id);
                return StatusCode(500, new { message = "Kampanya pasif edilirken hata oluştu." });
            }
        }

        /// <summary>
        /// Tüm aktif kampanyaların ürün indirimlerini yeniden hesaplar.
        /// Bakım veya toplu güncelleme için kullanılır.
        /// </summary>
        [HttpPost("recalculate-all")]
        [HasPermission(Permissions.Campaigns.Update)]
        public async Task<ActionResult<object>> RecalculateAllCampaigns()
        {
            try
            {
                _logger.LogInformation("Tüm kampanyalar yeniden hesaplanıyor...");
                
                var updatedCount = await _campaignApplicationService.RecalculateAllCampaignsAsync();
                
                _logger.LogInformation(
                    "Tüm kampanyalar yeniden hesaplandı. Güncellenen ürün: {UpdatedCount}",
                    updatedCount);
                
                return Ok(new 
                { 
                    success = true, 
                    updatedProductCount = updatedCount,
                    message = $"{updatedCount} ürünün indirimli fiyatı güncellendi."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kampanya yeniden hesaplama hatası");
                return StatusCode(500, new { message = "Kampanyalar yeniden hesaplanırken hata oluştu." });
            }
        }

        #endregion

        #region Yardımcı Endpoint'ler

        /// <summary>
        /// Kampanya türlerini listeler.
        /// Frontend dropdown için kullanılır.
        /// </summary>
        [HttpGet("types")]
        [HasPermission(Permissions.Campaigns.View)]
        public ActionResult<IEnumerable<object>> GetCampaignTypes()
        {
            var types = Enum.GetValues<CampaignType>()
                .Select(t => new 
                { 
                    value = (int)t, 
                    name = t.ToString(),
                    displayName = GetCampaignTypeDisplayName(t)
                })
                .ToList();
            
            return Ok(types);
        }

        /// <summary>
        /// Kampanya hedef türlerini listeler.
        /// Frontend dropdown için kullanılır.
        /// </summary>
        [HttpGet("target-types")]
        [HasPermission(Permissions.Campaigns.View)]
        public ActionResult<IEnumerable<object>> GetTargetTypes()
        {
            var types = Enum.GetValues<CampaignTargetType>()
                .Select(t => new 
                { 
                    value = (int)t, 
                    name = t.ToString(),
                    displayName = GetTargetTypeDisplayName(t)
                })
                .ToList();
            
            return Ok(types);
        }

        /// <summary>
        /// Kampanya için seçilebilir ürünleri listeler.
        /// </summary>
        [HttpGet("products")]
        [HasPermission(Permissions.Campaigns.View)]
        public async Task<ActionResult<IEnumerable<object>>> GetProductsForSelection()
        {
            try
            {
                var products = await _productRepository.GetAllAsync();
                var result = products
                    .Where(p => p.IsActive)
                    .Select(p => new 
                    { 
                        id = p.Id, 
                        name = p.Name,
                        categoryId = p.CategoryId,
                        price = p.Price
                    })
                    .OrderBy(p => p.name)
                    .ToList();
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ürün listesi getirme hatası");
                return StatusCode(500, new { message = "Ürünler yüklenirken hata oluştu." });
            }
        }

        /// <summary>
        /// Kampanya için seçilebilir kategorileri listeler.
        /// </summary>
        [HttpGet("categories")]
        [HasPermission(Permissions.Campaigns.View)]
        public async Task<ActionResult<IEnumerable<object>>> GetCategoriesForSelection()
        {
            try
            {
                var categories = await _categoryRepository.GetAllAsync();
                var result = categories
                    .Where(c => c.IsActive)
                    .Select(c => new 
                    { 
                        id = c.Id, 
                        name = c.Name,
                        parentId = c.ParentId
                    })
                    .OrderBy(c => c.name)
                    .ToList();
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kategori listesi getirme hatası");
                return StatusCode(500, new { message = "Kategoriler yüklenirken hata oluştu." });
            }
        }

        /// <summary>
        /// Aktif kampanya sayısını döndürür (dashboard için).
        /// </summary>
        [HttpGet("stats")]
        [HasPermission(Permissions.Campaigns.View)]
        public async Task<ActionResult<object>> GetCampaignStats()
        {
            try
            {
                var allCampaigns = await _campaignService.GetAllAsync();
                var activeCampaigns = await _campaignService.GetActiveCampaignsAsync();
                
                return Ok(new 
                {
                    total = allCampaigns.Count,
                    active = activeCampaigns.Count,
                    inactive = allCampaigns.Count - activeCampaigns.Count,
                    byType = allCampaigns
                        .GroupBy(c => c.Type)
                        .Select(g => new { type = g.Key.ToString(), count = g.Count() })
                        .ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kampanya istatistikleri getirme hatası");
                return StatusCode(500, new { message = "İstatistikler yüklenirken hata oluştu." });
            }
        }

        #endregion

        #region Kampanya Önizleme

        /// <summary>
        /// Kampanya önizlemesi - Kampanyanın etkileyeceği ürünleri ve fiyat değişikliklerini gösterir.
        /// Admin panelden "Önizle" butonuyla çağrılır.
        /// </summary>
        /// <param name="dto">Kampanya bilgileri (henüz kaydedilmemiş olabilir)</param>
        /// <returns>Etkilenecek ürünler, eski/yeni fiyatlar ve toplam indirim tutarı</returns>
        [HttpPost("preview")]
        [HasPermission(Permissions.Campaigns.View)]
        public async Task<ActionResult<CampaignPreviewResult>> PreviewCampaign([FromBody] CampaignSaveDto dto)
        {
            try
            {
                if (dto == null)
                {
                    return BadRequest(new { message = "Kampanya bilgileri gerekli." });
                }

                // Kampanya türüne göre fiyat hesaplama
                // Sadece Percentage ve FixedAmount türleri için önizleme yapılır
                if (dto.Type != CampaignType.Percentage && dto.Type != CampaignType.FixedAmount)
                {
                    return Ok(new CampaignPreviewResult
                    {
                        Message = $"{GetCampaignTypeDisplayName(dto.Type)} kampanyaları sepette uygulanır, ürün fiyatlarını değiştirmez.",
                        AffectedProducts = new List<CampaignPreviewProduct>(),
                        TotalDiscount = 0
                    });
                }

                // Hedef ürünleri bul
                var products = await GetTargetProductsAsync(dto);
                if (!products.Any())
                {
                    return Ok(new CampaignPreviewResult
                    {
                        Message = "Bu kampanya için hedef ürün bulunamadı.",
                        AffectedProducts = new List<CampaignPreviewProduct>(),
                        TotalDiscount = 0
                    });
                }

                // Her ürün için indirimli fiyat hesapla
                var affectedProducts = new List<CampaignPreviewProduct>();
                decimal totalDiscount = 0;

                foreach (var product in products)
                {
                    decimal discount = CalculateDiscount(dto, product.Price);
                    
                    // MaxDiscountAmount kontrolü
                    if (dto.MaxDiscountAmount.HasValue && discount > dto.MaxDiscountAmount.Value)
                    {
                        discount = dto.MaxDiscountAmount.Value;
                    }

                    decimal newPrice = product.Price - discount;
                    if (newPrice < 0) newPrice = 0;

                    affectedProducts.Add(new CampaignPreviewProduct
                    {
                        ProductId = product.Id,
                        ProductName = product.Name,
                        CategoryName = product.Category?.Name ?? "Kategori Yok",
                        OriginalPrice = product.Price,
                        NewPrice = newPrice,
                        DiscountAmount = discount,
                        DiscountPercentage = product.Price > 0 
                            ? Math.Round((discount / product.Price) * 100, 1) 
                            : 0
                    });

                    totalDiscount += discount;
                }

                // Sonuçları döndür
                return Ok(new CampaignPreviewResult
                {
                    Message = $"{affectedProducts.Count} ürün bu kampanyadan etkilenecek.",
                    AffectedProducts = affectedProducts
                        .OrderByDescending(p => p.DiscountAmount)
                        .ToList(),
                    TotalDiscount = totalDiscount,
                    TotalProductCount = affectedProducts.Count,
                    AverageDiscountPercentage = affectedProducts.Any() 
                        ? Math.Round(affectedProducts.Average(p => p.DiscountPercentage), 1) 
                        : 0
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kampanya önizleme hatası");
                return StatusCode(500, new { message = "Önizleme hesaplanırken hata oluştu." });
            }
        }

        /// <summary>
        /// Mevcut bir kampanyanın önizlemesini getirir.
        /// </summary>
        [HttpGet("{id:int}/preview")]
        [HasPermission(Permissions.Campaigns.View)]
        public async Task<ActionResult<CampaignPreviewResult>> PreviewExistingCampaign(int id)
        {
            try
            {
                var campaign = await _campaignService.GetByIdAsync(id);
                if (campaign == null)
                {
                    return NotFound(new { message = "Kampanya bulunamadı." });
                }

                // Campaign'ı DTO'ya çevir ve preview yap
                var dto = new CampaignSaveDto
                {
                    Name = campaign.Name,
                    Description = campaign.Description,
                    ImageUrl = campaign.ImageUrl,
                    StartDate = campaign.StartDate,
                    EndDate = campaign.EndDate,
                    IsActive = campaign.IsActive,
                    Type = campaign.Type,
                    TargetType = campaign.TargetType,
                    DiscountValue = campaign.DiscountValue,
                    MaxDiscountAmount = campaign.MaxDiscountAmount,
                    TargetIds = campaign.Targets?.Select(t => t.TargetId).ToList() ?? new List<int>()
                };

                // Mevcut preview metodunu çağır
                return await PreviewCampaign(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Mevcut kampanya önizleme hatası. ID: {CampaignId}", id);
                return StatusCode(500, new { message = "Önizleme hesaplanırken hata oluştu." });
            }
        }

        /// <summary>
        /// Kampanya hedeflerine göre ürünleri getirir (önizleme için).
        /// </summary>
        private async Task<List<Product>> GetTargetProductsAsync(CampaignSaveDto dto)
        {
            var products = new List<Product>();

            switch (dto.TargetType)
            {
                case CampaignTargetType.All:
                    // Tüm aktif ürünler
                    var allProducts = await _productRepository.GetAllAsync();
                    products = allProducts.Where(p => p.IsActive).ToList();
                    break;

                case CampaignTargetType.Product:
                    // Belirli ürünler - Toplu sorgulama ile N+1 query problemi önlendi
                    if (dto.TargetIds?.Any() == true)
                    {
                        // Tek seferde tüm ürünleri getir (N sorgu yerine 1 sorgu)
                        products = await _productRepository.GetByIdsAsync(dto.TargetIds);
                    }
                    break;

                case CampaignTargetType.Category:
                    // Belirli kategorilerdeki ürünler
                    if (dto.TargetIds?.Any() == true)
                    {
                        foreach (var categoryId in dto.TargetIds)
                        {
                            var categoryProducts = await _productRepository.GetByCategoryIdAsync(categoryId);
                            products.AddRange(categoryProducts.Where(p => p.IsActive));
                        }
                        // Tekrarları kaldır
                        products = products.GroupBy(p => p.Id).Select(g => g.First()).ToList();
                    }
                    break;
            }

            return products;
        }

        /// <summary>
        /// Kampanya türüne göre indirim tutarını hesaplar.
        /// </summary>
        private decimal CalculateDiscount(CampaignSaveDto dto, decimal price)
        {
            return dto.Type switch
            {
                CampaignType.Percentage => price * (dto.DiscountValue / 100m),
                CampaignType.FixedAmount => dto.DiscountValue,
                _ => 0
            };
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Kampanya DTO validasyonu yapar.
        /// </summary>
        private async Task<ActionResult?> ValidateCampaignDto(CampaignSaveDto? dto)
        {
            if (dto == null || !ModelState.IsValid)
            {
                return BadRequest(new { message = "Geçersiz kampanya verisi." });
            }

            if (dto.EndDate < dto.StartDate)
            {
                return BadRequest(new { message = "Bitiş tarihi başlangıç tarihinden önce olamaz." });
            }

            // Kampanya türüne göre ek validasyonlar
            switch (dto.Type)
            {
                case CampaignType.Percentage:
                    if (dto.DiscountValue <= 0 || dto.DiscountValue > 100)
                    {
                        return BadRequest(new { message = "Yüzde indirim 0-100 arasında olmalıdır." });
                    }
                    break;
                    
                case CampaignType.FixedAmount:
                    if (dto.DiscountValue <= 0)
                    {
                        return BadRequest(new { message = "İndirim tutarı 0'dan büyük olmalıdır." });
                    }
                    break;
                    
                case CampaignType.BuyXPayY:
                    if (!dto.BuyQty.HasValue || !dto.PayQty.HasValue)
                    {
                        return BadRequest(new { message = "X Al Y Öde kampanyası için BuyQty ve PayQty zorunludur." });
                    }
                    if (dto.PayQty >= dto.BuyQty)
                    {
                        return BadRequest(new { message = "Ödenecek adet, alınacak adetten küçük olmalıdır." });
                    }
                    break;
            }

            // Hedef tipi kontrolü
            if (dto.TargetType != CampaignTargetType.All && (dto.TargetIds == null || !dto.TargetIds.Any()))
            {
                return BadRequest(new { message = "Kategori veya ürün bazlı kampanyalar için en az bir hedef seçilmelidir." });
            }

            // ====================================================================
            // HEDEF ID VARLIK KONTROLÜ
            // Admin tarafından gönderilen ID'lerin gerçekten veritabanında
            // var olduğunu kontrol eder. Olmayan ID'ler için hata döner.
            // ====================================================================
            if (dto.TargetType == CampaignTargetType.Product && dto.TargetIds?.Any() == true)
            {
                var validProducts = await _productRepository.GetByIdsAsync(dto.TargetIds);
                var validProductIds = validProducts.Select(p => p.Id).ToHashSet();
                var invalidIds = dto.TargetIds.Where(id => !validProductIds.Contains(id)).ToList();

                if (invalidIds.Any())
                {
                    return BadRequest(new
                    {
                        message = "Geçersiz ürün ID'leri bulundu. Lütfen var olan aktif ürünler seçin.",
                        invalidIds = invalidIds,
                        invalidCount = invalidIds.Count
                    });
                }
            }
            else if (dto.TargetType == CampaignTargetType.Category && dto.TargetIds?.Any() == true)
            {
                var validCategories = await _categoryRepository.GetByIdsAsync(dto.TargetIds);
                var validCategoryIds = validCategories.Select(c => c.Id).ToHashSet();
                var invalidIds = dto.TargetIds.Where(id => !validCategoryIds.Contains(id)).ToList();

                if (invalidIds.Any())
                {
                    return BadRequest(new
                    {
                        message = "Geçersiz kategori ID'leri bulundu. Lütfen var olan aktif kategoriler seçin.",
                        invalidIds = invalidIds,
                        invalidCount = invalidIds.Count
                    });
                }
            }

            return null;
        }

        /// <summary>
        /// Campaign entity'sini CampaignListDto'ya dönüştürür.
        /// </summary>
        private static CampaignListDto MapToListDto(Campaign campaign)
        {
            return new CampaignListDto
            {
                Id = campaign.Id,
                Name = campaign.Name,
                Description = campaign.Description,
                ImageUrl = campaign.ImageUrl,
                StartDate = campaign.StartDate,
                EndDate = campaign.EndDate,
                IsActive = campaign.IsActive,
                // Yeni alanlar
                Type = campaign.Type,
                TargetType = campaign.TargetType,
                DiscountValue = campaign.DiscountValue
            };
        }

        /// <summary>
        /// Campaign entity'sini CampaignDetailDto'ya dönüştürür.
        /// Hedef isimleri de dahil edilir.
        /// </summary>
        private async Task<CampaignDetailDto> MapToDetailDtoAsync(Campaign campaign)
        {
            var detail = new CampaignDetailDto
            {
                Id = campaign.Id,
                Name = campaign.Name,
                Description = campaign.Description,
                ImageUrl = campaign.ImageUrl,
                StartDate = campaign.StartDate,
                EndDate = campaign.EndDate,
                IsActive = campaign.IsActive,
                // Yeni alanlar
                Type = campaign.Type,
                TargetType = campaign.TargetType,
                DiscountValue = campaign.DiscountValue,
                MaxDiscountAmount = campaign.MaxDiscountAmount,
                MinCartTotal = campaign.MinCartTotal,
                MinQuantity = campaign.MinQuantity,
                BuyQty = campaign.BuyQty,
                PayQty = campaign.PayQty,
                Priority = campaign.Priority,
                IsStackable = campaign.IsStackable
            };

            // Geriye dönük uyumluluk için eski alanları da doldur
            #pragma warning disable CS0618
            var firstRule = campaign.Rules?.FirstOrDefault();
            var firstReward = campaign.Rewards?.FirstOrDefault();
            
            detail.ConditionJson = firstRule?.ConditionJson;
            detail.RewardType = firstReward?.RewardType ?? "Percent";
            detail.RewardValue = firstReward?.Value ?? campaign.DiscountValue;

            if (campaign.Rules != null)
            {
                detail.RulesSummaries = campaign.Rules
                    .Where(r => !string.IsNullOrWhiteSpace(r.ConditionJson))
                    .Select(r => r.ConditionJson!)
                    .ToList();
            }

            if (campaign.Rewards != null)
            {
                detail.RewardsSummaries = campaign.Rewards
                    .Select(BuildRewardSummary)
                    .Where(s => !string.IsNullOrEmpty(s))
                    .ToList();
            }
            #pragma warning restore CS0618

            // Hedef bilgilerini getir
            if (campaign.Targets != null && campaign.Targets.Any())
            {
                var targets = new List<CampaignTargetDto>();
                
                foreach (var target in campaign.Targets)
                {
                    var targetDto = new CampaignTargetDto
                    {
                        TargetId = target.TargetId,
                        TargetKind = target.TargetKind
                    };

                    // Hedef ismini getir
                    if (target.TargetKind == CampaignTargetKind.Product)
                    {
                        var product = await _productRepository.GetByIdAsync(target.TargetId);
                        targetDto.TargetName = product?.Name;
                    }
                    else if (target.TargetKind == CampaignTargetKind.Category)
                    {
                        var category = await _categoryRepository.GetByIdAsync(target.TargetId);
                        targetDto.TargetName = category?.Name;
                    }

                    targets.Add(targetDto);
                }

                detail.Targets = targets;
            }

            return detail;
        }

        /// <summary>
        /// Kampanya türü için görüntüleme adı döndürür.
        /// </summary>
        private static string GetCampaignTypeDisplayName(CampaignType type) => type switch
        {
            CampaignType.Percentage => "Yüzde İndirim",
            CampaignType.FixedAmount => "Sabit Tutar İndirim",
            CampaignType.BuyXPayY => "X Al Y Öde",
            CampaignType.FreeShipping => "Ücretsiz Kargo",
            _ => "Bilinmiyor"
        };

        /// <summary>
        /// Hedef türü için görüntüleme adı döndürür.
        /// </summary>
        private static string GetTargetTypeDisplayName(CampaignTargetType type) => type switch
        {
            CampaignTargetType.All => "Tüm Ürünler",
            CampaignTargetType.Category => "Kategori Bazlı",
            CampaignTargetType.Product => "Ürün Bazlı",
            _ => "Bilinmiyor"
        };

        /// <summary>
        /// Kampanya ödülü için özet metin oluşturur.
        /// Geriye dönük uyumluluk için korundu.
        /// </summary>
        #pragma warning disable CS0618
        private static string BuildRewardSummary(CampaignReward reward)
        {
            if (reward == null)
            {
                return string.Empty;
            }

            var culture = new CultureInfo("tr-TR");
            return reward.RewardType?.ToLowerInvariant() switch
            {
                "amount" => $"₺{reward.Value.ToString("0.##", culture)} İndirim",
                "freeshipping" => "Ücretsiz Kargo",
                _ => $"%{reward.Value.ToString("0.##", culture)} İndirim"
            };
        }
        #pragma warning restore CS0618

        /// <summary>
        /// Dosya içeriğinin gerçekten image olup olmadığını magic number (file signature) ile kontrol eder.
        /// XSS ve malicious file upload saldırılarına karşı koruma sağlar.
        /// </summary>
        /// <param name="fileStream">Kontrol edilecek dosya stream'i</param>
        /// <returns>Geçerli image ise true, değilse false</returns>
        private static bool IsValidImageFile(Stream fileStream)
        {
            // Magic number (file signature) kontrol tablosu
            // Her image formatının benzersiz byte signature'ı vardır
            var imageSignatures = new Dictionary<string, byte[][]>
            {
                // JPEG: FF D8 FF
                { ".jpg", new[] { new byte[] { 0xFF, 0xD8, 0xFF } } },
                { ".jpeg", new[] { new byte[] { 0xFF, 0xD8, 0xFF } } },

                // PNG: 89 50 4E 47 0D 0A 1A 0A
                { ".png", new[] { new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A } } },

                // GIF: 47 49 46 38 (GIF8)
                { ".gif", new[] {
                    new byte[] { 0x47, 0x49, 0x46, 0x38, 0x37, 0x61 }, // GIF87a
                    new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61 }  // GIF89a
                }},

                // WebP: 52 49 46 46 ... 57 45 42 50 (RIFF...WEBP)
                { ".webp", new[] { new byte[] { 0x52, 0x49, 0x46, 0x46 } } }
            };

            try
            {
                // Dosyanın ilk 16 byte'ını oku (en uzun signature için yeterli)
                var headerBytes = new byte[16];
                fileStream.Position = 0;
                var bytesRead = fileStream.Read(headerBytes, 0, headerBytes.Length);

                if (bytesRead < 4)
                {
                    return false; // Çok küçük dosya
                }

                // Her image formatının signature'ını kontrol et
                foreach (var signatureSet in imageSignatures.Values)
                {
                    foreach (var signature in signatureSet)
                    {
                        if (headerBytes.Take(signature.Length).SequenceEqual(signature))
                        {
                            // Stream pozisyonunu başa al (tekrar kullanılabilir olması için)
                            fileStream.Position = 0;
                            return true;
                        }
                    }
                }

                // WebP için özel kontrol (RIFF ve WEBP arasında data var)
                if (headerBytes.Take(4).SequenceEqual(new byte[] { 0x52, 0x49, 0x46, 0x46 }) && bytesRead >= 12)
                {
                    // WEBP string'i 8. byte'tan sonra olmalı
                    if (headerBytes[8] == 0x57 && headerBytes[9] == 0x45 &&
                        headerBytes[10] == 0x42 && headerBytes[11] == 0x50)
                    {
                        fileStream.Position = 0;
                        return true;
                    }
                }

                fileStream.Position = 0;
                return false;
            }
            catch
            {
                fileStream.Position = 0;
                return false;
            }
        }

        /// <summary>
        /// Dosya adını güvenli hale getirir (path traversal saldırılarına karşı koruma).
        /// Tehlikeli karakterleri temizler ve benzersiz GUID ile unique filename oluşturur.
        /// </summary>
        /// <param name="originalFileName">Orijinal dosya adı</param>
        /// <returns>Güvenli, unique filename</returns>
        private static string SanitizeAndGenerateUniqueFileName(string originalFileName)
        {
            if (string.IsNullOrWhiteSpace(originalFileName))
            {
                return $"campaign_{Guid.NewGuid()}.jpg";
            }

            // Extension'ı ayır
            var extension = Path.GetExtension(originalFileName).ToLowerInvariant();

            // Geçerli extension kontrolü
            if (!AllowedExtensions.Contains(extension))
            {
                extension = ".jpg"; // Default
            }

            // Dosya adından extension'ı çıkar
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(originalFileName);

            // Path traversal karakterlerini temizle
            // ../../../etc/passwd gibi saldırıları engeller
            fileNameWithoutExtension = fileNameWithoutExtension
                .Replace("..", "")
                .Replace("/", "")
                .Replace("\\", "")
                .Replace(":", "")
                .Replace("*", "")
                .Replace("?", "")
                .Replace("\"", "")
                .Replace("<", "")
                .Replace(">", "")
                .Replace("|", "");

            // Sadece alphanumeric ve - _ . karakterlerini tut (Türkçe karakter desteği)
            fileNameWithoutExtension = new string(fileNameWithoutExtension
                .Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_' || c == '.')
                .Take(50) // Maksimum uzunluk
                .ToArray());

            // Boş kaldıysa default
            if (string.IsNullOrWhiteSpace(fileNameWithoutExtension))
            {
                fileNameWithoutExtension = "campaign";
            }

            // Benzersiz dosya adı oluştur (GUID ile)
            // Format: campaign_originalname_guid_timestamp.ext
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var uniqueId = Guid.NewGuid().ToString("N").Substring(0, 8); // İlk 8 karakter

            return $"campaign_{fileNameWithoutExtension}_{uniqueId}_{timestamp}{extension}";
        }

        #endregion
    }
}
