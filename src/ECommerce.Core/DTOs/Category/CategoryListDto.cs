// Konum: /Users/dilarasara/e-ticaret/src/ECommerce.Core/DTOs/Category/CategoryListDto.cs
using ECommerce.Core.DTOs;
using System;
using System.Collections.Generic;

namespace ECommerce.Core.DTOs.Category
{
    public class CategoryListDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public int? ParentId { get; set; }
    }
}