using ECommerce.Entities.Concrete;
using System.Collections.Generic;
using System;

namespace ECommerce.Entities.Concrete
{
    public class Discount : BaseEntity
    {
        public string Title { get; set; } = string.Empty;
        public bool IsPercentage { get; set; } = true;
        public decimal Value { get; set; } // % veya sabit tutar
        public DateTime StartDate { get; set; } = DateTime.UtcNow;
        public DateTime EndDate { get; set; }
        // IsActive already provided by BaseEntity
        public string? ConditionsJson { get; set; }

        // ✅ Many-to-many ilişki
        public virtual ICollection<Product> Products { get; set; } = new HashSet<Product>();

    }
}
