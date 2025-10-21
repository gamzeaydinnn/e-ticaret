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

    public List<OrderItemDto> OrderItems { get; set; } = new();
    }

    // OrderItemDetailDto kaldırıldı, ortak OrderItemDto kullanılacak
}
