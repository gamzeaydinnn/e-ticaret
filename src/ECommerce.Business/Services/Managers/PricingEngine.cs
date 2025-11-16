using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Core.DTOs.Pricing;
using ECommerce.Core.Interfaces;
using ECommerce.Entities.Concrete;

namespace ECommerce.Business.Services.Managers
{
    public class PricingEngine : IPricingEngine
    {
        private readonly IProductRepository _productRepository;
        private readonly IDiscountRepository _discountRepository;
        private readonly ICouponRepository _couponRepository;
        private readonly ICampaignService _campaignService;

        public PricingEngine(
            IProductRepository productRepository,
            IDiscountRepository discountRepository,
            ICouponRepository couponRepository,
            ICampaignService campaignService)
        {
            _productRepository = productRepository;
            _discountRepository = discountRepository;
            _couponRepository = couponRepository;
            _campaignService = campaignService;
        }

        public async Task<CartPricingResultDto> CalculateCartAsync(
            int? userId,
            IEnumerable<CartItemInputDto> items,
            string? couponCode)
        {
            var result = new CartPricingResultDto();
            var itemList = items?.ToList() ?? new List<CartItemInputDto>();
            if (!itemList.Any())
            {
                return result;
            }

            var productIds = itemList.Select(i => i.ProductId).Distinct().ToList();
            var allProducts = await _productRepository.GetAllAsync();
            var productsById = allProducts
                .Where(p => productIds.Contains(p.Id))
                .ToDictionary(p => p.Id);

            // Pre-load active discounts
            var activeDiscounts = await _discountRepository.GetActiveDiscountsAsync();

            foreach (var line in itemList)
            {
                if (!productsById.TryGetValue(line.ProductId, out var product))
                {
                    continue;
                }

                var unitPrice = product.SpecialPrice ?? product.Price;
                if (unitPrice < 0) unitPrice = 0;

                var lineBase = unitPrice * line.Quantity;
                var lineDiscount = 0m;

                // Apply product-level discounts (very simple: take the best discount that applies to this product)
                var productDiscounts = activeDiscounts
                    .Where(d => d.Products != null && d.Products.Any(p => p.Id == product.Id))
                    .ToList();

                if (productDiscounts.Any())
                {
                    var best = productDiscounts
                        .Select(d => ComputeDiscountAmount(lineBase, d.IsPercentage, d.Value))
                        .DefaultIfEmpty(0m)
                        .Max();
                    lineDiscount += best;
                }

                var lineFinal = lineBase - lineDiscount;
                if (lineFinal < 0) lineFinal = 0;

                result.Items.Add(new CartItemPricingDto
                {
                    ProductId = product.Id,
                    Name = product.Name,
                    Quantity = line.Quantity,
                    UnitPrice = unitPrice,
                    LineBaseTotal = lineBase,
                    LineDiscountTotal = lineDiscount,
                    LineFinalTotal = lineFinal
                });
            }

            result.Subtotal = result.Items.Sum(i => i.LineBaseTotal);

            // Apply campaigns (simple: if subtotal >= threshold, percent discount on whole basket)
            var campaigns = await _campaignService.GetActiveCampaignsAsync();
            foreach (var campaign in campaigns)
            {
                if (TryApplySimpleCampaign(campaign, result, out var campaignDiscount))
                {
                    result.CampaignDiscountTotal += campaignDiscount;
                    result.AppliedCampaignNames.Add(campaign.Name);
                }
            }

            // Apply coupon if provided and valid
            if (!string.IsNullOrWhiteSpace(couponCode))
            {
                var coupon = await _couponRepository.GetByCodeAsync(couponCode.Trim());
                if (coupon != null && coupon.IsActive && coupon.ExpirationDate >= DateTime.UtcNow)
                {
                    var eligibleBase = result.Subtotal - result.CampaignDiscountTotal;
                    if (!coupon.MinOrderAmount.HasValue || eligibleBase >= coupon.MinOrderAmount.Value)
                    {
                        var couponAmount = ComputeDiscountAmount(eligibleBase, coupon.IsPercentage, coupon.Value);
                        result.CouponDiscountTotal = couponAmount;
                        result.AppliedCouponCode = coupon.Code;
                    }
                }
            }

            // For now, leave delivery fee as 0. It can be filled by caller based on shipping method.
            result.DeliveryFee = 0m;

            var totalDiscount = result.CampaignDiscountTotal + result.CouponDiscountTotal;
            var grand = result.Subtotal - totalDiscount + result.DeliveryFee;
            result.GrandTotal = grand < 0 ? 0 : grand;

            return result;
        }

        private static decimal ComputeDiscountAmount(decimal baseAmount, bool isPercentage, decimal value)
        {
            if (baseAmount <= 0 || value <= 0) return 0m;
            if (isPercentage)
            {
                var perc = value / 100m;
                return Math.Round(baseAmount * perc, 2, MidpointRounding.AwayFromZero);
            }

            return Math.Min(baseAmount, value);
        }

        private static bool TryApplySimpleCampaign(Campaign campaign, CartPricingResultDto result, out decimal discount)
        {
            discount = 0m;
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
                discount = ComputeDiscountAmount(baseAmount, true, reward.Value);
                return discount > 0;
            }

            if (reward.RewardType.Equals("Amount", StringComparison.OrdinalIgnoreCase))
            {
                discount = ComputeDiscountAmount(baseAmount, false, reward.Value);
                return discount > 0;
            }

            if (reward.RewardType.Equals("FreeShipping", StringComparison.OrdinalIgnoreCase))
            {
                // Delivery fee 0 yapmayı fiyat motorunun dışında bırakıyoruz; burada sadece işaretleyebiliriz.
                // Şimdilik, FreeShipping için doğrudan 0 ek indirim geri dönmeyelim (delivery ayrı hesaplanacak).
                return false;
            }

            return false;
        }
    }
}

