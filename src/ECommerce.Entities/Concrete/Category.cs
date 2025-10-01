using System.Collections.Generic;

namespace ECommerce.Entities.Concrete
{
    public class Category : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public int? ParentId { get; set; }
        public int SortOrder { get; set; } = 0;
        public string Slug { get; set; } = string.Empty;
        //ParentCategoryId ile hiyerarşi desteği.
        public ICollection<Product> Products { get; set; } = new HashSet<Product>();

        // Navigation Properties
        public virtual Category? Parent { get; set; }
        public virtual ICollection<Category> SubCategories { get; set; } = new HashSet<Category>();}
}
