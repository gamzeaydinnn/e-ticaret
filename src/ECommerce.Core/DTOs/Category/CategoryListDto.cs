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
        public int SortOrder { get; set; } = 0;
        public bool IsActive { get; set; } = true;
        public string? MikroAnaGrupKod { get; set; }
        public string? MikroAltGrupKod { get; set; }
        public string? NavbarGroupKey { get; set; }
        public List<CategoryListDto>? SubCategories { get; set; }
    }
}
