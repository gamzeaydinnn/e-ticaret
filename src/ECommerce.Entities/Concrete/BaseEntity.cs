using System;
using System.Collections.Generic;
using ECommerce.Entities.Concrete;

namespace ECommerce.Entities.Concrete
{
    public abstract class BaseEntity
    {
        public int Id { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    }
}
