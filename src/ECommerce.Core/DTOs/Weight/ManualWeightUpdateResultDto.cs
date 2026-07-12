using ECommerce.Entities.Enums;

namespace ECommerce.Core.DTOs.Weight
{
    public class ManualWeightUpdateResultDto
    {
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public int OrderId { get; set; }
        public int OrderItemId { get; set; }
        public int? AdjustmentId { get; set; }
        public decimal EstimatedWeight { get; set; }
        public decimal ActualWeight { get; set; }
        public decimal PriceDifference { get; set; }
        public decimal FinalAmount { get; set; }
        public decimal PreAuthAmount { get; set; }
        public decimal MaxCaptureAmountFromPreAuth { get; set; }
        public bool ExceedsPreAuthLimit { get; set; }
        /// <summary>AuthCapture veya SaleImmediate</summary>
        public string PaymentFlowType { get; set; } = string.Empty;
        /// <summary>Sale modunda veya Auth×1.20 aşımında manuel tahsilat gerekir.</summary>
        public bool RequiresManualCollection { get; set; }
        /// <summary>Bankadan otomatik çekilemeyen fazla tutar (TL).</summary>
        public decimal UncollectableOverageAmount { get; set; }
        /// <summary>Sale modunda eksik gram için manuel iade gerekebilir.</summary>
        public bool RequiresManualRefund { get; set; }
        public WeightAdjustmentStatus AdjustmentStatus { get; set; }
    }
}
