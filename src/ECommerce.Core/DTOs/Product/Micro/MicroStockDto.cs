namespace ECommerce.Core.DTOs.Micro
{
    public class MicroStockDto
    {
        public int ProductId { get; set; }
        public int Stock { get; set; }
        public string Sku { get; set; } = string.Empty; // ekle
         public int Quantity { get; set; }
    }
}