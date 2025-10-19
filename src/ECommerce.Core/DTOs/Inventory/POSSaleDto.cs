namespace ECommerce.Core.DTOs.Inventory
{
    public class POSSaleDto
    {
        public string Sku { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public int? PerformedByUserId { get; set; }
        public string? Note { get; set; }
    }
}

