using System;
using ECommerce.Entities.Concrete;
using System.Collections.Generic;
using System;

namespace ECommerce.Entities.Concrete
{
    public enum InventoryChangeType { Purchase, Sale, Correction, Return, Transfer }

    public class InventoryLog : BaseEntity
    {
        public int ProductId { get; set; }
        public int ChangeQuantity { get; set; }
        public InventoryChangeType ChangeType { get; set; }
        public string? Note { get; set; }
        public int? PerformedByUserId { get; set; }
        // CreatedAt already provided by BaseEntity

        public virtual Product? Product { get; set; }
    }
}
