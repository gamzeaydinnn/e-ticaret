using ECommerce.Core.DTOs;
using System;
using System.Collections.Generic;

namespace ECommerce.Core.DTOs.Order
{
    // Sipariş oluşturma için kullanılan DTO
    public class OrderCreateDto
    {
        public int? UserId { get; set; } // Backend oturum açmış kullanıcıdan doldurur, guest siparişlerde null kalır
        public decimal TotalPrice { get; set; } // Toplam tutar (sunucuda yeniden hesaplanır)
        public List<OrderItemDto> OrderItems { get; set; } = new(); // Sipariş kalemleri (ortak DTO)

        // İstemci tarafı idempotent sipariş oluşturma anahtarı
        public Guid? ClientOrderId { get; set; }

        // Müşteri bilgileri
        public string? CustomerName { get; set; }
        public string? CustomerPhone { get; set; }
        public string? CustomerEmail { get; set; }

        // Teslimat adresi
        public string ShippingAddress { get; set; } = string.Empty;
        public string ShippingCity { get; set; } = string.Empty;
        public string? ShippingDistrict { get; set; }
        public string? ShippingPostalCode { get; set; }

        // Kargo bilgileri
        public string ShippingMethod { get; set; } = "car"; // car veya motorcycle
        public decimal ShippingCost { get; set; } = 30m; // Kargo ücreti

        // Teslimat notu/slot bilgisi (opsiyonel)
        public string? DeliveryNotes { get; set; }
    }
}
 
