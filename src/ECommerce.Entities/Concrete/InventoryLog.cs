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
        public string Action { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal OldStock { get; set; }
        public decimal NewStock { get; set; }
        public string? ReferenceId { get; set; }

        public virtual Product? Product { get; set; }
    }
}
