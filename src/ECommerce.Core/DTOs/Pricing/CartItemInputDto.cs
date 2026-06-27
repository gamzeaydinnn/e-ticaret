using System;

namespace ECommerce.Core.DTOs.Pricing
{
    public class CartItemInputDto
    {
        public int ProductId { get; set; }
        public decimal Quantity { get; set; }
    }
}

