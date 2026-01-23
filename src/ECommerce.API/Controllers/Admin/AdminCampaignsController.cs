using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Core.Constants;
using ECommerce.Core.DTOs.Promotions;
using ECommerce.Core.Interfaces;
using ECommerce.Entities.Concrete;
using ECommerce.Entities.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

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
    [Authorize(Roles = Roles.AdminLike)]
    [ApiController]
    [Route("api/admin/campaigns")]
    public class AdminCampaignsController : ControllerBase
    {
        private readonly ICampaignService _campaignService;
        private readonly IProductRepository _productRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly ILogger<AdminCampaignsController> _logger;

        public AdminCampaignsController(
            ICampaignService campaignService,
            IProductRepository productRepository,
            ICategoryRepository categoryRepository,
            ILogger<AdminCampaignsController> logger)
        {
            _campaignService = campaignService ?? throw new ArgumentNullException(nameof(campaignService));
            _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
            _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region CRUD Operasyonları

        /// <summary>
        /// Tüm kampanyaları listeler.
        /// </summary>
        [HttpGet]
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
        public async Task<ActionResult<CampaignDetailDto>> CreateCampaign([FromBody] CampaignSaveDto dto)
        {
            try
            {
                // Validasyon
                var validationResult = ValidateCampaignDto(dto);
                if (validationResult != null)
                {
                    return validationResult;
                }

                var created = await _campaignService.CreateAsync(dto);
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
        public async Task<ActionResult<CampaignDetailDto>> UpdateCampaign(int id, [FromBody] CampaignSaveDto dto)
        {
            try
            {
                // Validasyon
                var validationResult = ValidateCampaignDto(dto);
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
        public async Task<IActionResult> DeleteCampaign(int id)
        {
            try
            {
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

        #endregion

        #region Durum Yönetimi

        /// <summary>
        /// Kampanya aktif/pasif durumunu değiştirir.
        /// </summary>
        [HttpPatch("{id:int}/toggle")]
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

        #endregion

        #region Yardımcı Endpoint'ler

        /// <summary>
        /// Kampanya türlerini listeler.
        /// Frontend dropdown için kullanılır.
        /// </summary>
        [HttpGet("types")]
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

        #region Private Methods

        /// <summary>
        /// Kampanya DTO validasyonu yapar.
        /// </summary>
        private ActionResult? ValidateCampaignDto(CampaignSaveDto? dto)
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

        #endregion
    }
}
