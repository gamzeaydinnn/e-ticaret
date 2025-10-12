using ECommerce.Core.DTOs;
using System;
using System.Collections.Generic;

namespace ECommerce.Core.DTOs.Order

{
    public class OrderUpdateDto
    {
        public decimal TotalPrice { get; set; }      // Toplam tutar
        public string Status { get; set; } = string.Empty; // Sipariş durumu
        // Opsiyonel: teslimat veya fatura bilgileri eklenebilir
    }
}
