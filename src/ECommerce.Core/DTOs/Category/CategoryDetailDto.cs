// Konum: /Users/dilarasara/e-ticaret/src/ECommerce.Core/DTOs/Category/CategoryDetailDto.cs

using System.Collections.Generic;
using System;
using ECommerce.Core.DTOs;
namespace ECommerce.Core.DTOs.Category
{
    public class CategoryDetailDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }

        // Bu kategoriye ait alt kategorileri listelemek için
        public ICollection<CategoryListDto> SubCategories { get; set; } = new List<CategoryListDto>();
    }
}