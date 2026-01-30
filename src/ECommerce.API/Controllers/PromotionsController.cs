using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Core.DTOs.Promotions;
using ECommerce.Entities.Concrete;
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
        /// [KULLANIMI ÖNERİLMİYOR] Eski ücretsiz kargo kontrolü endpoint'i.
        /// Kategori bazlı kampanyalarda doğru çalışmaz.
        /// Yeni endpoint: POST /api/promotions/free-shipping
        /// </summary>
        /// <param name="cartTotal">Sepet tutarı</param>
        [Obsolete("Bu endpoint kategori bazlı kampanyaları desteklemez. POST /free-shipping endpoint'ini kullanın.")]
        [HttpGet("free-shipping")]
        public async Task<ActionResult<FreeShippingStatusDto>> CheckFreeShippingLegacy([FromQuery] decimal cartTotal)
        {
            _logger.LogWarning("Deprecated GET /free-shipping endpoint kullanıldı. POST endpoint'ine geçilmeli.");
            
            // Eski davranışı koru ama uyarı logla
            return await CheckFreeShippingInternal(cartTotal, null);
        }

        /// <summary>
        /// Ücretsiz kargo kampanyasını kontrol eder.
        /// Kategori ve ürün bazlı kampanyalar için sepet ürünlerini doğrular.
        /// 
        /// KRİTİK: Kategori bazlı kampanyalarda TÜM sepet ürünleri hedef kategoride olmalıdır.
        /// Farklı kategoriden ürün varsa ücretsiz kargo uygulanmaz.
        /// </summary>
        /// <param name="request">Sepet tutarı ve ürün bilgileri</param>
        [HttpPost("free-shipping")]
        public async Task<ActionResult<FreeShippingStatusDto>> CheckFreeShipping([FromBody] FreeShippingCheckRequest request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest(new { message = "İstek gövdesi boş olamaz." });
                }

                return await CheckFreeShippingInternal(request.CartTotal, request.Items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ücretsiz kargo kontrolü hatası. CartTotal: {CartTotal}, ItemCount: {ItemCount}", 
                    request?.CartTotal, request?.Items?.Count);
                return StatusCode(500, new { message = "Kargo kontrolü yapılırken hata oluştu." });
            }
        }

        /// <summary>
        /// Ücretsiz kargo kontrolü iç metodu.
        /// Hem eski (GET) hem yeni (POST) endpoint'ler tarafından kullanılır.
        /// </summary>
        private async Task<ActionResult<FreeShippingStatusDto>> CheckFreeShippingInternal(
            decimal cartTotal, 
            List<FreeShippingCartItemDto>? items)
        {
            try
            {
                // Aktif ücretsiz kargo kampanyalarını getir
                var activeCampaigns = await _campaignService.GetActiveCampaignsAsync();
                var freeShippingCampaigns = activeCampaigns
                    .Where(c => c.Type == CampaignType.FreeShipping)
                    .OrderBy(c => c.Priority)
                    .ToList();

                if (!freeShippingCampaigns.Any())
                {
                    return Ok(new FreeShippingStatusDto
                    { 
                        IsFreeShipping = false,
                        Message = "Ücretsiz kargo kampanyası bulunmuyor."
                    });
                }

                // Her kampanyayı kontrol et
                foreach (var campaign in freeShippingCampaigns)
                {
                    // 1. Minimum sepet tutarı kontrolü
                    if (campaign.MinCartTotal.HasValue && cartTotal < campaign.MinCartTotal.Value)
                    {
                        continue;
                    }

                    // 2. Hedef türüne göre validasyon
                    var isValid = await ValidateCampaignTargets(campaign, items);
                    if (!isValid)
                    {
                        continue;
                    }

                    // Kampanya geçerli - ücretsiz kargo uygulanabilir
                    return Ok(new FreeShippingStatusDto
                    { 
                        IsFreeShipping = true,
                        CampaignId = campaign.Id,
                        CampaignName = campaign.Name,
                        TargetType = campaign.TargetType,
                        TargetIds = campaign.Targets?.Select(t => t.TargetId).ToList(),
                        Message = "Ücretsiz kargo kazandınız!"
                    });
                }

                // Hiçbir kampanya uygun değil - en yakın kampanyayı bul
                var nearestCampaign = freeShippingCampaigns
                    .Where(c => c.MinCartTotal.HasValue && c.MinCartTotal > cartTotal)
                    .OrderBy(c => c.MinCartTotal)
                    .FirstOrDefault();

                if (nearestCampaign != null)
                {
                    var remaining = nearestCampaign.MinCartTotal!.Value - cartTotal;
                    var targetMessage = GetTargetMessage(nearestCampaign, items);
                    
                    return Ok(new FreeShippingStatusDto
                    { 
                        IsFreeShipping = false,
                        CampaignId = nearestCampaign.Id,
                        CampaignName = nearestCampaign.Name,
                        RemainingAmount = remaining,
                        MinCartTotal = nearestCampaign.MinCartTotal,
                        TargetType = nearestCampaign.TargetType,
                        TargetIds = nearestCampaign.Targets?.Select(t => t.TargetId).ToList(),
                        Message = $"Ücretsiz kargo için ₺{remaining:N2} daha eklemeniz gerekiyor.{targetMessage}"
                    });
                }

                // Kategori uyumsuzluğu mesajı
                var categoryMismatchCampaign = freeShippingCampaigns.FirstOrDefault(c => 
                    c.TargetType != CampaignTargetType.All);
                
                if (categoryMismatchCampaign != null && items?.Any() == true)
                {
                    return Ok(new FreeShippingStatusDto
                    { 
                        IsFreeShipping = false,
                        CampaignId = categoryMismatchCampaign.Id,
                        CampaignName = categoryMismatchCampaign.Name,
                        TargetType = categoryMismatchCampaign.TargetType,
                        TargetIds = categoryMismatchCampaign.Targets?.Select(t => t.TargetId).ToList(),
                        Message = "Sepetinizdeki tüm ürünler kampanya kapsamında değil. Ücretsiz kargo için sadece kampanya kapsamındaki ürünleri ekleyin."
                    });
                }

                return Ok(new FreeShippingStatusDto
                { 
                    IsFreeShipping = false,
                    Message = "Ücretsiz kargo koşulları sağlanmıyor."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ücretsiz kargo kontrolü hatası. CartTotal: {CartTotal}", cartTotal);
                return StatusCode(500, new { message = "Kargo kontrolü yapılırken hata oluştu." });
            }
        }

        /// <summary>
        /// Kampanya hedeflerini sepet ürünleriyle doğrular.
        /// Kategori bazlı kampanyalarda TÜM ürünler hedef kategoride olmalı.
        /// </summary>
        private Task<bool> ValidateCampaignTargets(Campaign campaign, List<FreeShippingCartItemDto>? items)
        {
            // TargetType = All ise herkes için geçerli
            if (campaign.TargetType == CampaignTargetType.All)
            {
                return Task.FromResult(true);
            }

            // Sepet boşsa veya ürün bilgisi yoksa (eski endpoint) - uyarı ver ama geçir
            if (items == null || !items.Any())
            {
                _logger.LogWarning(
                    "Kategori/ürün bazlı kampanya ({CampaignId}) için sepet ürün bilgisi eksik. " +
                    "Doğrulama atlanıyor - POST endpoint kullanılmalı.", 
                    campaign.Id);
                return Task.FromResult(false); // Güvenlik için false döndür
            }

            var targetIds = campaign.Targets?.Select(t => t.TargetId).ToHashSet() ?? new HashSet<int>();
            
            if (!targetIds.Any())
            {
                _logger.LogWarning("Kampanya ({CampaignId}) hedefleri tanımlı değil.", campaign.Id);
                return Task.FromResult(false);
            }

            switch (campaign.TargetType)
            {
                case CampaignTargetType.Category:
                    // TÜM ürünler hedef kategorilerden birinde olmalı
                    var allInCategory = items.All(item => targetIds.Contains(item.CategoryId));
                    if (!allInCategory)
                    {
                        _logger.LogDebug(
                            "Kampanya ({CampaignId}): Sepetteki bazı ürünler hedef kategorilerde değil. " +
                            "Hedef kategoriler: [{TargetIds}], Sepet kategorileri: [{CartCategories}]",
                            campaign.Id,
                            string.Join(", ", targetIds),
                            string.Join(", ", items.Select(i => i.CategoryId).Distinct()));
                    }
                    return Task.FromResult(allInCategory);

                case CampaignTargetType.Product:
                    // TÜM ürünler hedef ürünlerden biri olmalı
                    var allInProducts = items.All(item => targetIds.Contains(item.ProductId));
                    if (!allInProducts)
                    {
                        _logger.LogDebug(
                            "Kampanya ({CampaignId}): Sepetteki bazı ürünler hedef ürünlerde değil. " +
                            "Hedef ürünler: [{TargetIds}], Sepet ürünleri: [{CartProducts}]",
                            campaign.Id,
                            string.Join(", ", targetIds),
                            string.Join(", ", items.Select(i => i.ProductId).Distinct()));
                    }
                    return Task.FromResult(allInProducts);

                default:
                    return Task.FromResult(false);
            }
        }

        /// <summary>
        /// Kategori/ürün bazlı kampanyalar için ek mesaj oluşturur.
        /// </summary>
        private string GetTargetMessage(Campaign campaign, List<FreeShippingCartItemDto>? items)
        {
            if (campaign.TargetType == CampaignTargetType.All)
            {
                return string.Empty;
            }

            if (items == null || !items.Any())
            {
                return string.Empty;
            }

            var targetIds = campaign.Targets?.Select(t => t.TargetId).ToHashSet() ?? new HashSet<int>();
            
            if (campaign.TargetType == CampaignTargetType.Category)
            {
                var outOfScopeCount = items.Count(item => !targetIds.Contains(item.CategoryId));
                if (outOfScopeCount > 0)
                {
                    return $" (Sepetinizde {outOfScopeCount} ürün kampanya kategorisi dışında)";
                }
            }
            else if (campaign.TargetType == CampaignTargetType.Product)
            {
                var outOfScopeCount = items.Count(item => !targetIds.Contains(item.ProductId));
                if (outOfScopeCount > 0)
                {
                    return $" (Sepetinizde {outOfScopeCount} ürün kampanya kapsamı dışında)";
                }
            }

            return string.Empty;
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
        
        /// <summary>
        /// Kampanya hedef türü (bilgilendirme amaçlı).
        /// All: Tüm ürünler, Category: Belirli kategoriler, Product: Belirli ürünler
        /// </summary>
        public CampaignTargetType? TargetType { get; set; }
        
        /// <summary>
        /// Kampanya hedef ID'leri (kategori veya ürün ID'leri)
        /// </summary>
        public List<int>? TargetIds { get; set; }
    }

    /// <summary>
    /// Ücretsiz kargo kontrolü için istek DTO'su.
    /// Kategori bazlı kampanyalarda sepet ürünlerinin doğrulanması için gerekli.
    /// </summary>
    public class FreeShippingCheckRequest
    {
        /// <summary>
        /// Sepet toplam tutarı
        /// </summary>
        public decimal CartTotal { get; set; }
        
        /// <summary>
        /// Sepet ürünleri (kategori validasyonu için zorunlu)
        /// </summary>
        public List<FreeShippingCartItemDto> Items { get; set; } = new();
    }

    /// <summary>
    /// Ücretsiz kargo kontrolü için sepet ürünü DTO'su
    /// </summary>
    public class FreeShippingCartItemDto
    {
        /// <summary>
        /// Ürün ID
        /// </summary>
        public int ProductId { get; set; }
        
        /// <summary>
        /// Kategori ID (kampanya hedef kontrolü için)
        /// </summary>
        public int CategoryId { get; set; }
        
        /// <summary>
        /// Miktar
        /// </summary>
        public int Quantity { get; set; }
        
        /// <summary>
        /// Birim fiyat
        /// </summary>
        public decimal UnitPrice { get; set; }
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
