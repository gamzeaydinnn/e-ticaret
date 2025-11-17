using System;
using ECommerce.Core.DTOs;
using System;
using System.Collections.Generic;
namespace ECommerce.Core.DTOs.Order
{
    public class OrderListDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public bool IsGuestOrder { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public decimal VatAmount { get; set; }
        public decimal TotalPrice { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal FinalPrice { get; set; }
        public decimal CouponDiscountAmount { get; set; }
        public decimal CampaignDiscountAmount { get; set; }
        public string? CouponCode { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
    public int TotalItems { get; set; } // Toplam ürün sayısı

    // Detaylar
    public string ShippingMethod { get; set; } = string.Empty;
    public decimal ShippingCost { get; set; }
    public string ShippingAddress { get; set; } = string.Empty;
        public string? TrackingNumber { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string DeliveryNotes { get; set; } = string.Empty;
    public List<OrderItemDto> OrderItems { get; set; } = new();
    }
}
//admin/kullanıcı için farklı projection.??
