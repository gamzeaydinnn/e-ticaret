using ECommerce.Core.DTOs;
using System;
using System.Collections.Generic;
namespace ECommerce.Core.DTOs.Product
{
    public class ProductCreateDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal? SpecialPrice { get; set; }  // Yeni alan
        public int StockQuantity { get; set; }
        
        // Stock property - StockQuantity ile senkronize
        public int Stock 
        { 
            get => StockQuantity; 
            set => StockQuantity = value; 
        }
        
        public int CategoryId { get; set; }

        public string? ImageUrl { get; set; }

        public int? BrandId { get; set; } // Brand ilişkisi için id
    }
}
