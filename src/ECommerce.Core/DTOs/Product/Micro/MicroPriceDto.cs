namespace ECommerce.Core.DTOs.Micro
{
    public class MicroPriceDto
    {
        public string Sku { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Currency { get; set; } = "TRY";
        public DateTime? EffectiveDate { get; set; }
    }
}

