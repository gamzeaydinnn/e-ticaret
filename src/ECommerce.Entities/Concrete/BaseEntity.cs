using System;

namespace ECommerce.Entities.Concrete
{
    public abstract class BaseEntity
    {
        public int Id { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedDate { get; set; }
        public bool IsActive { get; set; } = true;
    
        DateTime CreatedAt { get; set; }
        DateTime? UpdatedAt { get; set; }
    }
}
