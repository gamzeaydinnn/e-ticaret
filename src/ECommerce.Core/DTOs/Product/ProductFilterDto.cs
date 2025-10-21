using System.Collections.Generic;

namespace ECommerce.Core.DTOs.Product
{
    public class ProductFilterDto
    {
        public string? Query { get; set; }
        public List<int>? CategoryIds { get; set; }
        public List<int>? BrandIds { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public bool? InStockOnly { get; set; }
        public int? MinRating { get; set; } // 1..5
        public string? SortBy { get; set; } // name|price|created
        public string? SortDir { get; set; } // asc|desc
        public int Page { get; set; } = 1;
        public int Size { get; set; } = 12;
    }
}

