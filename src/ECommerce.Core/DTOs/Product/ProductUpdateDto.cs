using ECommerce.Core.DTOs;
using System;
using System.Collections.Generic;
namespace ECommerce.Core.DTOs.Product
{
    public class ProductUpdateDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal? SpecialPrice { get; set; }  // Yeni alan
        public int StockQuantity { get; set; }
        public int CategoryId { get; set; }

        public string? ImageUrl { get; set; }

        /// <summary>
        /// Yeni eklenecek görseller — mevcut görseller silinmez, yalnızca eklenir.
        /// </summary>
        public List<string>? AdditionalImageUrls { get; set; }

        /// <summary>
        /// Güncelleme sonrası nihai görsel listesi — silinenler listede yoksa kaldırılır.
        /// </summary>
        public List<string>? ImageUrls { get; set; }

        public int? BrandId { get; set; } // Brand ilişkisi için id
        public bool? AdminOverrideName { get; set; }
        public bool? AdminOverridePrice { get; set; }
        public bool? AdminOverrideCategory { get; set; }

        /// <summary>
        /// SKU bazlı güncelleme için — Mikro ERP ürünlerinde id=0 olduğundan
        /// SKU üzerinden eşleştirme yapılır.
        /// </summary>
        public string? SKU { get; set; }
    }
}
