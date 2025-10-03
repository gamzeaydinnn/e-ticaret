//ödeme + adres + sepet.
//CheckoutDto → sepeti finalize etmek, shipping, billing info vs. içerir.
namespace ECommerce.Core.DTOs.Payment
{
    public class CheckoutDto
    {
        public int OrderId { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string Currency { get; set; } = "TRY";

        // Ek alanlar
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string? ShippingAddress { get; set; }
        public string? BillingAddress { get; set; }
    }
}
