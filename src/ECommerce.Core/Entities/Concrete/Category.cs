using System.Collections.Generic;
namespace ECommerce.Core.Entities.Concrete
{
    public class Category : IBaseEntity
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Slug { get; set; }
        public int? ParentId { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public ICollection<Product> Products { get; set; }
    }
}
