// ==========================================================================
// RefundDiagnosticDtos.cs - Faz 0 iade teşhis raporu DTO'ları
// ==========================================================================

using System;
using System.Collections.Generic;

namespace ECommerce.Core.DTOs.Order
{
    /// <summary>
    /// Tek sipariş için iade teşhis raporu.
    /// Admin panelinde "neden fail oldu?" sorusuna yanıt verir.
    /// </summary>
    public class RefundOrderDiagnosticDto
    {
        public int OrderId { get; set; }
        public string? OrderNumber { get; set; }
        public string OrderStatus { get; set; } = string.Empty;
        public string? PaymentMethod { get; set; }
        public decimal FinalPrice { get; set; }
        public decimal CapturedAmount { get; set; }
        public decimal MaxRefundableAmount { get; set; }
        public decimal AlreadyRefundedAmount { get; set; }
        public decimal RemainingRefundableAmount { get; set; }
        public bool IsAuthOnly { get; set; }
        public bool IsSameBusinessDayAsPayment { get; set; }
        public string RecommendedOperation { get; set; } = string.Empty;
        public List<string> Issues { get; set; } = new();
        public List<RefundPaymentDiagnosticDto> Payments { get; set; } = new();
        public List<RefundRequestDiagnosticDto> RefundRequests { get; set; } = new();
    }

    public class RefundPaymentDiagnosticDto
    {
        public int PaymentId { get; set; }
        public string? TransactionType { get; set; }
        public string? Status { get; set; }
        public decimal Amount { get; set; }
        public decimal? RefundedAmount { get; set; }
        public decimal? CapturedAmount { get; set; }
        public bool HasHostLogKey { get; set; }
        public string? HostLogKeyMasked { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class RefundRequestDiagnosticDto
    {
        public int RefundRequestId { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal RefundAmount { get; set; }
        public string? TransactionType { get; set; }
        public string? RefundFailureReason { get; set; }
        public DateTime? ProcessedAt { get; set; }
    }

    /// <summary>
    /// Başarısız iade taleplerinin toplu teşhis özeti.
    /// </summary>
    public class RefundFailedSummaryDto
    {
        public int FailedCount { get; set; }
        public List<RefundOrderDiagnosticDto> Items { get; set; } = new();
    }
}
