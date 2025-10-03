//	• Payments (Id, OrderId, Provider, ProviderPaymentId, Amount, Status, CreatedAt, RawResponse)
using System;

namespace ECommerce.Entities.Concrete
{
    public class Payments
    {
        public int Id { get; set; } // Primary Key
        public int OrderId { get; set; } // Hangi siparişe ait
        public string Provider { get; set; } = null!; // Ödeme sağlayıcı (Stripe, Iyzico, PayPal vs.)
        public string ProviderPaymentId { get; set; } = null!; // Sağlayıcıdaki ödeme id'si
        public decimal Amount { get; set; } // Tutar
        public string Status { get; set; } = "Pending"; // Pending, Success, Failed
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // Oluşturulma tarihi
        public string? RawResponse { get; set; } // API'den gelen ham json
    }
}
