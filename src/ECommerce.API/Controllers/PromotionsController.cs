using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Core.DTOs.Promotions;
using ECommerce.Entities.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ECommerce.API.Controllers
{
    /// <summary>
    /// Public kampanya sistemi controller'ı.
    /// Müşteri arayüzünde kampanya badge'leri, sepet indirimleri ve ücretsiz kargo için kullanılır.
    /// Bu endpoint'ler herkese açıktır (authentication gerektirmez).
    /// 
    /// NOT: CampaignsController (banner kampanyaları) ile karıştırılmamalı.
    /// Bu controller veritabanındaki dinamik kampanyaları yönetir.
    /// </summary>
    [ApiController]
    [Route("api/promotions")]
    public class PromotionsController : ControllerBase
    {
        private readonly ICampaignService _campaignService;
        private readonly ILogger<PromotionsController> _logger;

        public PromotionsController(
            ICampaignService campaignService,
            ILogger<PromotionsController> logger)
        {
            _campaignService = campaignService ?? throw new ArgumentNullException(nameof(campaignService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Aktif kampanyaları listeler.
        /// Ana sayfa banner'ları ve kampanya listeleme için kullanılır.
        /// </summary>
        /// <returns>Aktif kampanya listesi</returns>
        [HttpGet("active")]
        public async Task<ActionResult<IEnumerable<ActiveCampaignDto>>> GetActiveCampaigns()
        {
            try
            {
                var campaigns = await _campaignService.GetActiveCampaignsAsync();
                
                var result = campaigns.Select(c => new ActiveCampaignDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Description = c.Description,
                    Type = c.Type,
                    TargetType = c.TargetType,
                    TargetIds = c.Targets?.Select(t => t.TargetId).ToList() ?? new List<int>(),
                    TargetKinds = c.Targets?.Select(t => t.TargetKind).ToList() ?? new List<CampaignTargetKind>(),
                    DiscountValue = c.DiscountValue,
                    BuyQty = c.BuyQty,
                    PayQty = c.PayQty,
                    MinCartTotal = c.MinCartTotal,
                    StartDate = c.StartDate,
                    EndDate = c.EndDate,
                    DisplayText = GetCampaignDisplayText(c.Type, c.DiscountValue, c.BuyQty, c.PayQty),
                    BadgeText = GetCampaignBadgeText(c.Type, c.DiscountValue, c.BuyQty, c.PayQty),
                    BadgeColor = GetCampaignBadgeColor(c.Type)
                }).ToList();

                _logger.LogDebug("Aktif kampanyalar getirildi. Toplam: {Count}", result.Count);
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Aktif kampanyalar getirme hatası");
                return StatusCode(500, new { message = "Kampanyalar yüklenirken hata oluştu." });
            }
        }

        /// <summary>
        /// Belirli bir ürün için geçerli kampanyaları listeler.
        /// Ürün detay sayfası ve sepet için kullanılır.
        /// </summary>
        /// <param name="productId">Ürün ID</param>
        /// <param name="categoryId">Kategori ID</param>
        [HttpGet("product/{productId:int}")]
        public async Task<ActionResult<IEnumerable<ActiveCampaignDto>>> GetCampaignsForProduct(
            int productId, 
            [FromQuery] int? categoryId = null)
        {
            try
            {
                // Kategori ID yoksa sadece ürün bazlı ve tüm ürünler kampanyalarını getir
                var campaigns = await _campaignService.GetApplicableCampaignsForProductAsync(
                    productId, 
                    categoryId ?? 0);
                
                var result = campaigns.Select(c => new ActiveCampaignDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Description = c.Description,
                    Type = c.Type,
                    TargetType = c.TargetType,
                    TargetIds = c.Targets?.Select(t => t.TargetId).ToList() ?? new List<int>(),
                    TargetKinds = c.Targets?.Select(t => t.TargetKind).ToList() ?? new List<CampaignTargetKind>(),
                    DiscountValue = c.DiscountValue,
                    BuyQty = c.BuyQty,
                    PayQty = c.PayQty,
                    MinCartTotal = c.MinCartTotal,
                    StartDate = c.StartDate,
                    EndDate = c.EndDate,
                    DisplayText = GetCampaignDisplayText(c.Type, c.DiscountValue, c.BuyQty, c.PayQty),
                    BadgeText = GetCampaignBadgeText(c.Type, c.DiscountValue, c.BuyQty, c.PayQty),
                    BadgeColor = GetCampaignBadgeColor(c.Type)
                }).ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ürün kampanyaları getirme hatası. ProductId: {ProductId}", productId);
                return StatusCode(500, new { message = "Kampanyalar yüklenirken hata oluştu." });
            }
        }

        /// <summary>
        /// Belirli bir kategori için geçerli kampanyaları listeler.
        /// Kategori sayfası için kullanılır.
        /// </summary>
        /// <param name="categoryId">Kategori ID</param>
        [HttpGet("category/{categoryId:int}")]
        public async Task<ActionResult<IEnumerable<ActiveCampaignDto>>> GetCampaignsForCategory(int categoryId)
        {
            try
            {
                // Kategori için geçerli kampanyaları getir (ürün ID = 0 vererek sadece kategori bazlı olanları al)
                var campaigns = await _campaignService.GetApplicableCampaignsForProductAsync(0, categoryId);
                
                var result = campaigns.Select(c => new ActiveCampaignDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Description = c.Description,
                    Type = c.Type,
                    TargetType = c.TargetType,
                    TargetIds = c.Targets?.Select(t => t.TargetId).ToList() ?? new List<int>(),
                    TargetKinds = c.Targets?.Select(t => t.TargetKind).ToList() ?? new List<CampaignTargetKind>(),
                    DiscountValue = c.DiscountValue,
                    BuyQty = c.BuyQty,
                    PayQty = c.PayQty,
                    MinCartTotal = c.MinCartTotal,
                    StartDate = c.StartDate,
                    EndDate = c.EndDate,
                    DisplayText = GetCampaignDisplayText(c.Type, c.DiscountValue, c.BuyQty, c.PayQty),
                    BadgeText = GetCampaignBadgeText(c.Type, c.DiscountValue, c.BuyQty, c.PayQty),
                    BadgeColor = GetCampaignBadgeColor(c.Type)
                }).ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kategori kampanyaları getirme hatası. CategoryId: {CategoryId}", categoryId);
                return StatusCode(500, new { message = "Kampanyalar yüklenirken hata oluştu." });
            }
        }

        /// <summary>
        /// Ücretsiz kargo kampanyasını kontrol eder.
        /// Sepet sayfasında kullanılır.
        /// </summary>
        /// <param name="cartTotal">Sepet tutarı</param>
        [HttpGet("free-shipping")]
        public async Task<ActionResult<FreeShippingStatusDto>> CheckFreeShipping([FromQuery] decimal cartTotal)
        {
            try
            {
                var campaign = await _campaignService.GetFreeShippingCampaignAsync(cartTotal);
                
                if (campaign != null)
                {
                    return Ok(new FreeShippingStatusDto
                    { 
                        IsFreeShipping = true,
                        CampaignId = campaign.Id,
                        CampaignName = campaign.Name,
                        Message = "Ücretsiz kargo kazandınız!"
                    });
                }

                // Ücretsiz kargo için en yakın kampanyayı bul
                var allFreeShippingCampaigns = await _campaignService.GetActiveCampaignsAsync();
                var nearestFreeShippingCampaign = allFreeShippingCampaigns
                    .Where(c => c.Type == CampaignType.FreeShipping && c.MinCartTotal.HasValue)
                    .OrderBy(c => c.MinCartTotal)
                    .FirstOrDefault(c => c.MinCartTotal > cartTotal);

                if (nearestFreeShippingCampaign != null)
                {
                    var remaining = nearestFreeShippingCampaign.MinCartTotal!.Value - cartTotal;
                    return Ok(new FreeShippingStatusDto
                    { 
                        IsFreeShipping = false,
                        RemainingAmount = remaining,
                        MinCartTotal = nearestFreeShippingCampaign.MinCartTotal,
                        Message = $"Ücretsiz kargo için ₺{remaining:N2} daha eklemeniz gerekiyor."
                    });
                }

                return Ok(new FreeShippingStatusDto
                { 
                    IsFreeShipping = false,
                    Message = "Ücretsiz kargo kampanyası bulunmuyor."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ücretsiz kargo kontrolü hatası. CartTotal: {CartTotal}", cartTotal);
                return StatusCode(500, new { message = "Kargo kontrolü yapılırken hata oluştu." });
            }
        }

        /// <summary>
        /// Kampanya özet bilgilerini döndürür (dashboard için).
        /// </summary>
        [HttpGet("summary")]
        public async Task<ActionResult<PromotionsSummaryDto>> GetPromotionsSummary()
        {
            try
            {
                var activeCampaigns = await _campaignService.GetActiveCampaignsAsync();
                
                return Ok(new PromotionsSummaryDto
                {
                    ActiveCampaignCount = activeCampaigns.Count,
                    HasFreeShipping = activeCampaigns.Any(c => c.Type == CampaignType.FreeShipping),
                    HasPercentageDiscount = activeCampaigns.Any(c => c.Type == CampaignType.Percentage),
                    HasBuyXPayY = activeCampaigns.Any(c => c.Type == CampaignType.BuyXPayY),
                    CampaignTypes = activeCampaigns
                        .GroupBy(c => c.Type)
                        .Select(g => new CampaignTypeCount 
                        { 
                            Type = g.Key.ToString(), 
                            Count = g.Count() 
                        })
                        .ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kampanya özeti getirme hatası");
                return StatusCode(500, new { message = "Kampanya özeti yüklenirken hata oluştu." });
            }
        }

        #region Private Helper Methods

        /// <summary>
        /// Kampanya türüne göre görüntüleme metni oluşturur.
        /// </summary>
        private static string GetCampaignDisplayText(
            CampaignType type, 
            decimal discountValue, 
            int? buyQty, 
            int? payQty)
        {
            return type switch
            {
                CampaignType.Percentage => $"%{discountValue:0} İndirim",
                CampaignType.FixedAmount => $"₺{discountValue:N2} İndirim",
                CampaignType.BuyXPayY when buyQty.HasValue && payQty.HasValue => 
                    $"{buyQty} Al {payQty} Öde",
                CampaignType.FreeShipping => "Ücretsiz Kargo",
                _ => "Kampanya"
            };
        }

        /// <summary>
        /// Kampanya badge metni oluşturur (kısa versiyon).
        /// </summary>
        private static string GetCampaignBadgeText(
            CampaignType type, 
            decimal discountValue, 
            int? buyQty, 
            int? payQty)
        {
            return type switch
            {
                CampaignType.Percentage => $"%{discountValue:0}",
                CampaignType.FixedAmount => $"-₺{discountValue:0}",
                CampaignType.BuyXPayY when buyQty.HasValue && payQty.HasValue => 
                    $"{buyQty}={payQty}",
                CampaignType.FreeShipping => "🚚",
                _ => "🎁"
            };
        }

        /// <summary>
        /// Kampanya türüne göre badge rengi döndürür.
        /// </summary>
        private static string GetCampaignBadgeColor(CampaignType type)
        {
            return type switch
            {
                CampaignType.Percentage => "danger",      // Kırmızı
                CampaignType.FixedAmount => "warning",    // Sarı
                CampaignType.BuyXPayY => "success",       // Yeşil
                CampaignType.FreeShipping => "info",      // Mavi
                _ => "secondary"
            };
        }

        #endregion
    }

    #region DTO Classes

    /// <summary>
    /// Aktif kampanya DTO (public endpoint için)
    /// </summary>
    public class ActiveCampaignDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public CampaignType Type { get; set; }
        public CampaignTargetType TargetType { get; set; }
        public List<int> TargetIds { get; set; } = new();
        public List<CampaignTargetKind> TargetKinds { get; set; } = new();
        public decimal DiscountValue { get; set; }
        public int? BuyQty { get; set; }
        public int? PayQty { get; set; }
        public decimal? MinCartTotal { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        
        /// <summary>
        /// Tam görüntüleme metni (örn: "%10 İndirim", "3 Al 2 Öde")
        /// </summary>
        public string DisplayText { get; set; } = string.Empty;
        
        /// <summary>
        /// Kısa badge metni (örn: "%10", "3=2")
        /// </summary>
        public string BadgeText { get; set; } = string.Empty;
        
        /// <summary>
        /// Bootstrap badge rengi (danger, warning, success, info)
        /// </summary>
        public string BadgeColor { get; set; } = "secondary";
    }

    /// <summary>
    /// Ücretsiz kargo durumu DTO
    /// </summary>
    public class FreeShippingStatusDto
    {
        public bool IsFreeShipping { get; set; }
        public int? CampaignId { get; set; }
        public string? CampaignName { get; set; }
        public decimal? RemainingAmount { get; set; }
        public decimal? MinCartTotal { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// Kampanya özeti DTO
    /// </summary>
    public class PromotionsSummaryDto
    {
        public int ActiveCampaignCount { get; set; }
        public bool HasFreeShipping { get; set; }
        public bool HasPercentageDiscount { get; set; }
        public bool HasBuyXPayY { get; set; }
        public List<CampaignTypeCount> CampaignTypes { get; set; } = new();
    }

    /// <summary>
    /// Kampanya türü sayacı
    /// </summary>
    public class CampaignTypeCount
    {
        public string Type { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    #endregion
}
