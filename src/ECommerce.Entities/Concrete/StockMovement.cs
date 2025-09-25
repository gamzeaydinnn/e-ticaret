using System;

namespace ECommerce.Core.Entities.Concrete
{
    public class StockMovement
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public int QuantityChange { get; set; }
        public DateTime MovementDate { get; set; }
        public string Reason { get; set; } // Örn: "Sale", "Return", "Manual"
    }
}
