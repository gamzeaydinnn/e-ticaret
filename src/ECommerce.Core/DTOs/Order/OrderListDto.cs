using System;

namespace ECommerce.Core.DTOs.Order
{
    public class OrderListDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public decimal TotalPrice { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public int TotalItems { get; set; } // Toplam ürün sayısı
    }
}
