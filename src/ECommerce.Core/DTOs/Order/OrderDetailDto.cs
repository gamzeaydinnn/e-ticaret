using System;
using System.Collections.Generic;
using ECommerce.Core.DTOs;


namespace ECommerce.Core.DTOs.Order
{
    public class OrderDetailDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public decimal TotalPrice { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }

        public List<OrderItemDetailDto> OrderItems { get; set; } = new();
    }

    public class OrderItemDetailDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice => UnitPrice * Quantity; // Hesaplanmış alan
    }
}
