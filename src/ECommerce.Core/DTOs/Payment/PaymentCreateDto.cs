//PaymentCreateDto → sadece ödeme başlatma için.
namespace ECommerce.Core.DTOs.Payment
{
    public class PaymentCreateDto
    {
        public int OrderId { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string Currency { get; set; } = "TRY";
    }
}
