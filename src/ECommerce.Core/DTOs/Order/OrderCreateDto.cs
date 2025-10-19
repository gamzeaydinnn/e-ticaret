using System;
using System.Collections.Generic;
using ECommerce.Core.DTOs;


namespace ECommerce.Core.DTOs.Order
{
    public class OrderCreateDto
    {
        public int? UserId { get; set; }              // Üye değilse null
        public decimal TotalPrice { get; set; }       // Toplam tutar (sunucuda yeniden hesaplanır)
        public List<OrderItemDto> OrderItems { get; set; } = new(); // Kalemler

        // Guest/Müşteri bilgileri
        public string? CustomerName { get; set; }
        public string? CustomerPhone { get; set; }
        public string? CustomerEmail { get; set; }

        // Teslimat adresi
        public string ShippingAddress { get; set; } = string.Empty;
        public string ShippingCity { get; set; } = string.Empty;
        public string? ShippingDistrict { get; set; }
        public string? ShippingPostalCode { get; set; }
    }

    public class OrderItemDto
    {
        public int ProductId { get; set; }           // Ürün ID
        public int Quantity { get; set; }            // Miktar
        public decimal UnitPrice { get; set; }       // Birim fiyat (istek için opsiyonel, sunucu esas alır)
    }
}

 
