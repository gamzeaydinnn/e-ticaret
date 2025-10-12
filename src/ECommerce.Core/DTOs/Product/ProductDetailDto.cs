//kategori, varyant, stok, resim listesi.
using System.Collections.Generic;
using ECommerce.Core.DTOs;
using System;
using System.Collections.Generic;
namespace ECommerce.Core.DTOs.Product
{
    public class ProductDetailDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        
        public string? Brand { get; set; }
        public string? ImageUrl { get; set; }

        // Kategori bilgisi
        public int CategoryId { get; set; }
        public string? CategoryName { get; set; }

        // Varyantlar
        public List<ProductVariantDto> Variants { get; set; } = new List<ProductVariantDto>();

        // Ürün resimleri (ekstra resimler)
        public List<string> ImageUrls { get; set; } = new List<string>();
    }

    // Varyant bilgisi için ayrı DTO
    public class ProductVariantDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
    }
}
