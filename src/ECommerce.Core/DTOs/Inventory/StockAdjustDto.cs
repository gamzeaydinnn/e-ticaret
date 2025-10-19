namespace ECommerce.Core.DTOs.Inventory
{
    public class StockAdjustDto
    {
        public string Sku { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string Reason { get; set; } = "Correction"; // Purchase/Correction/Return
        public int? PerformedByUserId { get; set; }
        public string? Note { get; set; }
    }
}

