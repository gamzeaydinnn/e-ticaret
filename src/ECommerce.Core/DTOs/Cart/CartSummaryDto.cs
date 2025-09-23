using System.Collections.Generic;
namespace ECommerce.Core.DTOs.Cart
{
    public class CartSummaryDto
    {
        public List<CartItemDto> Items { get; set; }
        public decimal Total { get; set; }
    }
}
