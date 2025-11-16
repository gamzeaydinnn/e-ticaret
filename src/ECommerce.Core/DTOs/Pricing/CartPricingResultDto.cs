using System;
using System.Collections.Generic;

namespace ECommerce.Core.DTOs.Pricing
{
    public class CartPricingResultDto
    {
        public List<CartItemPricingDto> Items { get; set; } = new();
        public decimal Subtotal { get; set; }
        public decimal CampaignDiscountTotal { get; set; }
        public decimal CouponDiscountTotal { get; set; }
        public decimal DeliveryFee { get; set; }
        public decimal GrandTotal { get; set; }
        public string? AppliedCouponCode { get; set; }
        public List<string> AppliedCampaignNames { get; set; } = new();
    }
}

