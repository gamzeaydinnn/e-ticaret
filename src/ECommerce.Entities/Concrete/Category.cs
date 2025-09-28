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
        public int Id { get; set; }
        public string Slug { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public ICollection<Product> Products { get; set; }

        // Navigation Properties
        public virtual Category? Parent { get; set; }
        public virtual ICollection<Category> SubCategories { get; set; } = new HashSet<Category>();
    }
}
