using ECommerce.Core.DTOs;
using System;
using System.Collections.Generic;

namespace ECommerce.Core.DTOs.Order

{
    public class OrderUpdateDto
    {
        public decimal? TotalPrice { get; set; }
        public string? Status { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerPhone { get; set; }
        public string? CustomerEmail { get; set; }
        public string? ShippingAddress { get; set; }
        public string? DeliveryNotes { get; set; }
        public string? TrackingNumber { get; set; }
        public List<OrderItemUpdateDto> OrderItems { get; set; } = new();
    }

    public class OrderItemUpdateDto
    {
        public int OrderItemId { get; set; }
        public int Quantity { get; set; }
    }
}
