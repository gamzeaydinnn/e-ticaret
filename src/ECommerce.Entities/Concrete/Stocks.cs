using System;
using ECommerce.Entities.Concrete;
using System.Collections.Generic;
using System;

namespace ECommerce.Entities.Concrete
{
    public class Stocks
    {
        public int Id { get; set; }

        /// <summary>
        /// Hangi ürün varyantına ait
        /// </summary>
        public int ProductVariantId { get; set; }

        /// <summary>
        /// Hangi depoda (Warehouse)
        /// </summary>
        public int WarehouseId { get; set; }

        /// <summary>
        /// Mevcut stok adedi
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        /// Ayrılmış (rezerv) stok adedi (örn: sipariş için tutuluyor)
        /// </summary>
        public int ReservedQuantity { get; set; }

        /// <summary>
        /// Yeniden sipariş verilecek minimum stok seviyesi
        /// </summary>
        public int ReorderLevel { get; set; }

        /// <summary>
        /// Son güncelleme tarihi
        /// </summary>
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        // Opsiyonel navigasyon property’leri
        public ProductVariant? ProductVariant { get; set; }
        //public Warehouse? Warehouse { get; set; }
        
    }
}
