using System;

namespace ECommerce.Core.DTOs.Pricing
{
    public class CartItemPricingDto
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineBaseTotal { get; set; }
        public decimal LineDiscountTotal { get; set; }
        public decimal LineFinalTotal { get; set; }
    }
}

