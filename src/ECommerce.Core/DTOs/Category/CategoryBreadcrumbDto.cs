using System;

namespace ECommerce.Core.DTOs.Category
{
    /// <summary>
    /// Kategori hiyerarşi ve breadcrumb DTO'su
    /// Üst ve alt kategori ilişkilerini formatlanmış breadcrumb ile taşır
    /// Örnek: "Gıda > Süt Ürünleri" veya "Süt Ürünleri"
    /// </summary>
    public class CategoryBreadcrumbDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public bool IsSubcategory { get; set; }

        /// <summary>
        /// Üst kategori bilgisi (eğer bu bir alt kategori ise)
        /// </summary>
        public CategoryBreadcrumbDto? Parent { get; set; }

        /// <summary>
        /// Formatlanmış hiyerarşi metni
        /// Örnekler: "Süt Ürünleri", "Gıda > Süt Ürünleri"
        /// </summary>
        public string Breadcrumb { get; set; } = string.Empty;
    }
}

