using System;

namespace ECommerce.Entities.Concrete
{
    public class ReconciliationLog
    {
        public int Id { get; set; }
        public string Provider { get; set; } = string.Empty;
        public string ProviderPaymentId { get; set; } = string.Empty;
        public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
        public string Issue { get; set; } = string.Empty; // description of mismatch
        public string? Details { get; set; }
    }
}
