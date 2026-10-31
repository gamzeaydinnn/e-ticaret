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
        public int? ParentId { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; } = true;
        public string? MikroAnaGrupKod { get; set; }
        public string? MikroAltGrupKod { get; set; }
        public string? NavbarGroupKey { get; set; }

        // Bu kategoriye ait alt kategorileri listelemek için
        public ICollection<CategoryListDto> SubCategories { get; set; } = new List<CategoryListDto>();
    }
}
