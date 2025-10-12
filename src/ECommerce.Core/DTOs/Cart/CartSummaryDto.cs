using System.Collections.Generic;
using ECommerce.Core.DTOs;
using System;
using System.Collections.Generic;
namespace ECommerce.Core.DTOs.Cart
{
    public class CartSummaryDto
    {
        public List<CartItemDto> Items { get; set; } = new();
        public decimal Total { get; set; }
    }
}
