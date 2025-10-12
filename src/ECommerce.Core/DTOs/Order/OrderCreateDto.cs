using System;
using System.Collections.Generic;
using ECommerce.Core.DTOs;


namespace ECommerce.Core.DTOs.Order
{
    public class OrderCreateDto
    {
        public int UserId { get; set; }              // Siparişi veren kullanıcı
        public decimal TotalPrice { get; set; }      // Toplam tutar
        public List<OrderItemDto> OrderItems { get; set; } = new();
 // Sipariş kalemleri

        // Opsiyonel: teslimat veya fatura bilgileri eklenebilir
    }

    public class OrderItemDto
    {
        public int ProductId { get; set; }           // Ürün ID
        public int Quantity { get; set; }            // Miktar
        public decimal UnitPrice { get; set; }       // Birim fiyat
    }
}

 