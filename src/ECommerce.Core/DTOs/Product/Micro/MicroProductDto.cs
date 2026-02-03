using ECommerce.Core.DTOs;
using System;
using System.Collections.Generic;
namespace ECommerce.Core.DTOs.Micro
{
    /// <summary>
    /// Mikro ERP ürün DTO'su.
    /// MikroAPI'den alınan veya gönderilen ürün bilgilerini temsil eder.
    /// </summary>
    public class MicroProductDto
    {
        public int Id { get; set; }
        public string Sku { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Stock { get; set; }

        // Mikro entegrasyonu için ek alanlar
        public string? Barcode { get; set; }
        public decimal VatRate { get; set; } = 20;
        public int StockQuantity { get; set; }
        public string? CategoryCode { get; set; }
        public string Unit { get; set; } = "ADET";
        public bool IsActive { get; set; } = true;
        public DateTime? LastModified { get; set; }
    }
}
