using System.Collections.Generic;

namespace ECommerce.Core.DTOs.Category
{
    /// <summary>
    /// Hiyerarşik kategori ağacı için DTO
    /// Alt kategoriler children listesinde recursive olarak bulunur
    /// </summary>
    public class CategoryTreeDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public int? ParentId { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; }
        
        /// <summary>
        /// Bu kategoriye ait ürün sayısı
        /// </summary>
        public int ProductCount { get; set; }
        
        /// <summary>
        /// Alt kategoriler (recursive)
        /// </summary>
        public List<CategoryTreeDto> Children { get; set; } = new List<CategoryTreeDto>();
    }
}
