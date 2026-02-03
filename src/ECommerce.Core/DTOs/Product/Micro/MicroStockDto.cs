using ECommerce.Core.DTOs;
using System;
using System.Collections.Generic;
namespace ECommerce.Core.DTOs.Micro
{
    /// <summary>
    /// Mikro ERP stok DTO'su.
    /// MikroAPI'den alınan stok bilgilerini temsil eder.
    /// </summary>
    public class MicroStockDto
    {
        public int ProductId { get; set; }
        public int Stock { get; set; }
        public string Sku { get; set; } = string.Empty;
        public int Quantity { get; set; }

        // Mikro entegrasyonu için ek alanlar
        public string? Barcode { get; set; }
        public int AvailableQuantity { get; set; }
        public int ReservedQuantity { get; set; }
        public string WarehouseCode { get; set; } = "DEPO1";
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}
