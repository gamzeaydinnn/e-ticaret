using System;
using System.Collections.Generic;
using ECommerce.Core.DTOs;


namespace ECommerce.Core.DTOs.Order
{
    public class OrderDetailDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public bool IsGuestOrder { get; set; }
        public decimal VatAmount { get; set; }
        public decimal TotalPrice { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal FinalPrice { get; set; }
        public decimal CouponDiscountAmount { get; set; }
        public decimal CampaignDiscountAmount { get; set; }
        public string? CouponCode { get; set; }
        public string? TrackingNumber { get; set; }
        public string Status { get; set; } = string.Empty;
        
        // Ödeme durumu bilgileri - Admin panel filtreleri için gerekli
        public string PaymentStatus { get; set; } = string.Empty;
        public bool IsPaid { get; set; }
        
        public DateTime OrderDate { get; set; }

    public List<OrderItemDto> OrderItems { get; set; } = new();
    }

    // OrderItemDetailDto kaldırıldı, ortak OrderItemDto kullanılacak
}
