using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Core.DTOs.Pricing;
using ECommerce.Core.DTOs.Promotions;
using ECommerce.Core.Interfaces;
using ECommerce.Entities.Concrete;
using ECommerce.Entities.Enums;
using Microsoft.Extensions.Logging;

namespace ECommerce.Business.Services.Managers
{
    /// <summary>
    /// Fiyatlandırma motoru.
    /// Sepet için tüm fiyat hesaplamalarını yapar:
    /// 1. Ürün bazlı indirimler (özel fiyat, miktar indirimi)
    /// 2. Kampanya indirimleri (Percentage, FixedAmount, BuyXPayY, FreeShipping)
    /// 3. Kupon indirimi (sipariş toplamı bazlı)
    /// 4. Kargo ücreti hesaplama
    /// 
    /// İndirim önceliği:
    /// - Kampanya: Satır bazlı (her ürün için en yüksek indirimi veren kampanya seçilir)
    /// - Kupon: Sipariş toplamı bazlı (kampanya sonrası tutara uygulanır)
    /// - İkisi birlikte çalışır (kampanya + kupon)
    /// </summary>
    public class PricingEngine : IPricingEngine
    {
        private readonly IProductRepository _productRepository;
        private readonly IDiscountRepository _discountRepository;
        private readonly ICouponRepository _couponRepository;
        private readonly ICampaignService _campaignService;
        private readonly ILogger<PricingEngine>? _logger;

        public PricingEngine(
            IProductRepository productRepository,
            IDiscountRepository discountRepository,
            ICouponRepository couponRepository,
            ICampaignService campaignService,
            ILogger<PricingEngine>? logger = null)
        {
            _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
            _discountRepository = discountRepository ?? throw new ArgumentNullException(nameof(discountRepository));
            _couponRepository = couponRepository ?? throw new ArgumentNullException(nameof(couponRepository));
            _campaignService = campaignService ?? throw new ArgumentNullException(nameof(campaignService));
            _logger = logger;
        }

        /// <summary>
        /// Sepet fiyatlandırması hesaplar.
        /// 
        /// Algoritma:
        /// 1. Ürün bilgilerini getir
        /// 2. Her satır için birim fiyat ve ürün indirimi hesapla
        /// 3. Kampanya indirimlerini hesapla (CampaignManager kullanarak)
        /// 4. Kupon indirimini hesapla (varsa)
        /// 5. Kargo ücreti ve FreeShipping kontrolü
        /// 6. Genel toplamı hesapla
        /// </summary>
        public async Task<CartPricingResultDto> CalculateCartAsync(
            int? userId,
            IEnumerable<CartItemInputDto> items,
            string? couponCode)
        {
            var result = new CartPricingResultDto();
            var itemList = items?.ToList() ?? new List<CartItemInputDto>();
            
            // Boş sepet kontrolü
            if (!itemList.Any())
            {
                _logger?.LogDebug("Boş sepet, fiyatlandırma atlandı");
                return result;
            }

            try
            {
                // 1. Ürün bilgilerini getir
                var productIds = itemList.Select(i => i.ProductId).Distinct().ToList();
                var allProducts = await _productRepository.GetAllAsync();
                var productsById = allProducts
                    .Where(p => productIds.Contains(p.Id))
                    .ToDictionary(p => p.Id);

                // 2. Aktif ürün indirimlerini getir (ürün bazlı, kampanya harici)
                var activeDiscounts = await _discountRepository.GetActiveDiscountsAsync();

                // Kampanya hesaplaması için ürün listesi hazırla
                var itemsForCampaign = new List<CartItemForCampaign>();

                // 3. Her satır için birim fiyat ve ürün indirimi hesapla
                foreach (var line in itemList)
                {
                    if (!productsById.TryGetValue(line.ProductId, out var product))
                    {
                        _logger?.LogWarning("Ürün bulunamadı: {ProductId}", line.ProductId);
                        continue;
                    }

                    // Birim fiyat: özel fiyat varsa onu kullan
                    var unitPrice = product.SpecialPrice ?? product.Price;
                    if (unitPrice < 0) unitPrice = 0;

                    var lineBase = unitPrice * line.Quantity;
                    var lineProductDiscount = 0m;

                    // Ürün bazlı indirimler (discount tablosundan)
                    var productDiscounts = activeDiscounts
                        .Where(d => d.Products != null && d.Products.Any(p => p.Id == product.Id))
                        .ToList();

                    if (productDiscounts.Any())
                    {
                        var best = productDiscounts
                            .Select(d => ComputeDiscountAmount(lineBase, d.IsPercentage, d.Value, null))
                            .DefaultIfEmpty(0m)
                            .Max();
                        lineProductDiscount = best;
                    }

                    // Satır sonucu ekle (kampanya indirimi sonra hesaplanacak)
                    result.Items.Add(new CartItemPricingDto
                    {
                        ProductId = product.Id,
                        CategoryId = product.CategoryId,
                        Name = product.Name,
                        Quantity = line.Quantity,
                        UnitPrice = unitPrice,
                        LineBaseTotal = lineBase,
                        LineDiscountTotal = lineProductDiscount,
                        LineCampaignDiscount = 0, // Aşağıda hesaplanacak
                        LineFinalTotal = lineBase - lineProductDiscount
                    });

                    // Kampanya hesaplaması için ürün bilgisi
                    itemsForCampaign.Add(new CartItemForCampaign
                    {
                        ProductId = product.Id,
                        CategoryId = product.CategoryId,
                        ProductName = product.Name,
                        UnitPrice = unitPrice,
                        Quantity = line.Quantity
                    });
                }

                // Ara toplam hesapla
                result.Subtotal = result.Items.Sum(i => i.LineBaseTotal);

                // 4. Kampanya indirimlerini hesapla (YENİ SİSTEM)
                var campaignResult = await _campaignService.CalculateCampaignDiscountsAsync(
                    itemsForCampaign, 
                    result.Subtotal);

                // Kampanya sonuçlarını satırlara uygula
                ApplyCampaignResultsToItems(result, campaignResult);

                // 5. Kupon indirimi hesapla
                if (!string.IsNullOrWhiteSpace(couponCode))
                {
                    await ApplyCouponAsync(result, couponCode.Trim());
                }

                // 6. Kargo ücreti (şimdilik 0, dış sistemden gelecek)
                // FreeShipping kampanyası varsa kargo ücreti zaten 0 olacak
                result.DeliveryFee = 0m;
                
                if (campaignResult.IsFreeShipping)
                {
                    result.IsFreeShipping = true;
                    var freeShippingCampaign = campaignResult.AppliedCampaigns
                        .FirstOrDefault(c => c.Type == CampaignType.FreeShipping);
                    if (freeShippingCampaign != null)
                    {
                        result.FreeShippingCampaignName = freeShippingCampaign.CampaignName;
                    }
                }

                // 7. Genel toplam hesapla
                var totalDiscount = result.CampaignDiscountTotal + result.CouponDiscountTotal;
                var grand = result.Subtotal - totalDiscount + result.DeliveryFee;
                result.GrandTotal = Math.Max(0, grand);

                _logger?.LogInformation(
                    "Sepet fiyatlandırması tamamlandı. " +
                    "Subtotal: {Subtotal}, Kampanya: {CampaignDiscount}, Kupon: {CouponDiscount}, " +
                    "Kargo: {Delivery}, Toplam: {GrandTotal}, FreeShipping: {FreeShipping}",
                    result.Subtotal, result.CampaignDiscountTotal, result.CouponDiscountTotal,
                    result.DeliveryFee, result.GrandTotal, result.IsFreeShipping);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Sepet fiyatlandırma hatası");
                // Hata durumunda basit hesaplama yap (kampanya/kupon olmadan)
                result.GrandTotal = result.Subtotal + result.DeliveryFee;
            }

            return result;
        }

        /// <summary>
        /// Kampanya sonuçlarını sepet satırlarına uygular.
        /// Her satıra ait kampanya indirimi ve bilgilerini ekler.
        /// </summary>
        private void ApplyCampaignResultsToItems(CartPricingResultDto result, CampaignCalculationResult campaignResult)
        {
            // Kampanya toplamını güncelle
            result.CampaignDiscountTotal = campaignResult.TotalCampaignDiscount;
            
            // Uygulanan kampanyaları ekle
            result.AppliedCampaigns = campaignResult.AppliedCampaigns;
            
            // Geriye dönük uyumluluk için kampanya adlarını da ekle
            result.AppliedCampaignNames = campaignResult.AppliedCampaigns
                .Select(c => c.CampaignName)
                .Distinct()
                .ToList();

            // Her satıra kampanya bilgilerini ekle
            foreach (var item in result.Items)
            {
                // Bu ürüne uygulanan kampanya indirimi
                if (campaignResult.LineDiscounts.TryGetValue(item.ProductId, out var lineDiscount))
                {
                    item.LineCampaignDiscount = lineDiscount;
                    item.LineFinalTotal = item.LineBaseTotal - item.LineDiscountTotal - lineDiscount;
                    
                    // Bu ürüne uygulanan kampanya bilgisi
                    var appliedCampaign = campaignResult.AppliedCampaigns
                        .FirstOrDefault(c => c.AppliedToItemIds.Contains(item.ProductId));
                    
                    if (appliedCampaign != null)
                    {
                        item.AppliedCampaignId = appliedCampaign.CampaignId;
                        item.AppliedCampaignName = appliedCampaign.CampaignName;
                        item.AppliedCampaignType = appliedCampaign.Type;
                        item.CampaignDisplayText = appliedCampaign.DisplayText;
                    }
                }
            }
        }

        /// <summary>
        /// Kupon indirimini uygular.
        /// Kupon sipariş toplamı bazlıdır (kampanya sonrası tutara uygulanır).
        /// </summary>
        private async Task ApplyCouponAsync(CartPricingResultDto result, string couponCode)
        {
            try
            {
                var coupon = await _couponRepository.GetByCodeAsync(couponCode);
                
                if (coupon == null)
                {
                    _logger?.LogDebug("Kupon bulunamadı: {CouponCode}", couponCode);
                    return;
                }

                // Kupon geçerlilik kontrolü
                if (!coupon.IsActive)
                {
                    _logger?.LogDebug("Kupon aktif değil: {CouponCode}", couponCode);
                    return;
                }

                if (coupon.ExpirationDate < DateTime.UtcNow)
                {
                    _logger?.LogDebug("Kupon süresi dolmuş: {CouponCode}", couponCode);
                    return;
                }

                // Kupon minimum tutar kontrolü (kampanya sonrası tutara bakılır)
                var eligibleBase = result.Subtotal - result.CampaignDiscountTotal;
                
                if (coupon.MinOrderAmount.HasValue && eligibleBase < coupon.MinOrderAmount.Value)
                {
                    _logger?.LogDebug(
                        "Kupon minimum tutarı karşılanmıyor. Gerekli: {MinAmount}, Mevcut: {CurrentAmount}",
                        coupon.MinOrderAmount.Value, eligibleBase);
                    return;
                }

                // Kupon indirimini hesapla
                var couponAmount = ComputeDiscountAmount(
                    eligibleBase, 
                    coupon.IsPercentage, 
                    coupon.Value,
                    coupon.MaxDiscountAmount);

                result.CouponDiscountTotal = couponAmount;
                result.AppliedCouponCode = coupon.Code;

                _logger?.LogDebug(
                    "Kupon uygulandı: {CouponCode}, İndirim: {DiscountAmount}",
                    couponCode, couponAmount);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Kupon uygulama hatası: {CouponCode}", couponCode);
            }
        }

        /// <summary>
        /// İndirim tutarını hesaplar.
        /// Yüzdelik veya sabit tutar indirim destekler.
        /// Maksimum indirim limiti uygulanabilir.
        /// </summary>
        private static decimal ComputeDiscountAmount(
            decimal baseAmount, 
            bool isPercentage, 
            decimal value,
            decimal? maxDiscountAmount)
        {
            if (baseAmount <= 0 || value <= 0) return 0m;

            decimal discount;
            
            if (isPercentage)
            {
                var perc = value / 100m;
                discount = Math.Round(baseAmount * perc, 2, MidpointRounding.AwayFromZero);
            }
            else
            {
                discount = Math.Min(baseAmount, value);
            }

            // Maksimum indirim limiti kontrolü
            if (maxDiscountAmount.HasValue && discount > maxDiscountAmount.Value)
            {
                discount = maxDiscountAmount.Value;
            }

            return discount;
        }

        #region Eski Metod (Geriye Dönük Uyumluluk için Korundu)
        
        /// <summary>
        /// [DEPRECATED] Eski basit kampanya uygulama metodu.
        /// Yeni sistem CampaignManager.CalculateCampaignDiscountsAsync() kullanır.
        /// Bu metod geriye dönük uyumluluk için korunmuştur.
        /// </summary>
        [Obsolete("Yeni kampanya sistemi için CampaignManager.CalculateCampaignDiscountsAsync() kullanın")]
        private static bool TryApplySimpleCampaign(Campaign campaign, CartPricingResultDto result, out decimal discount)
        {
            discount = 0m;
            
            #pragma warning disable CS0618
            if (campaign.Rewards == null || !campaign.Rewards.Any())
                return false;

            var reward = campaign.Rewards.First();

            // Try to read a simple threshold from ConditionJson: { "minSubtotal": 250 }
            decimal? minSubtotal = null;
            if (!string.IsNullOrWhiteSpace(campaign.Rules?.FirstOrDefault()?.ConditionJson))
            {
                try
                {
                    var json = campaign.Rules.First().ConditionJson;
                    var doc = System.Text.Json.JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("minSubtotal", out var el) && el.TryGetDecimal(out var val))
                    {
                        minSubtotal = val;
                    }
                }
                catch
                {
                    // invalid JSON -> ignore condition and treat as always-on
                }
            }

            if (minSubtotal.HasValue && result.Subtotal < minSubtotal.Value)
                return false;

            var baseAmount = result.Subtotal;

            if (reward.RewardType.Equals("Percent", StringComparison.OrdinalIgnoreCase))
            {
                discount = ComputeDiscountAmount(baseAmount, true, reward.Value, null);
                return discount > 0;
            }

            if (reward.RewardType.Equals("Amount", StringComparison.OrdinalIgnoreCase))
            {
                discount = ComputeDiscountAmount(baseAmount, false, reward.Value, null);
                return discount > 0;
            }

            if (reward.RewardType.Equals("FreeShipping", StringComparison.OrdinalIgnoreCase))
            {
                // Kargo ücreti hesabı dış sistemde yapılacak
                return false;
            }
            #pragma warning restore CS0618

            return false;
        }
        
        #endregion
    }
}

