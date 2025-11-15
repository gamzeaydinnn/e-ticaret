using System;

namespace ECommerce.Core.DTOs.Payment
{
    public class PaymentRefundRequestDto
    {
        public string PaymentId { get; set; } = string.Empty;
        public decimal? Amount { get; set; }
        public string? Reason { get; set; }
        public string? Provider { get; set; }
    }
}

