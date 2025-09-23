using System;
using System.Collections.Generic;
using ECommerce.Core.Entities.Concrete; // Add this if OrderItem is in the same namespace
// If OrderItem is in a different namespace, replace with the correct namespace, e.g.:
// using ECommerce.Core.Entities.OtherNamespace;

namespace ECommerce.Core.Entities.Concrete
{
    public class Order : IBaseEntity
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public decimal TotalAmount { get; set; }
        public string Currency { get; set; } = "TRY";
        public string Status { get; set; } = "Pending";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public ICollection<OrderItem> Items { get; set; }
    }
}
