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
        public WeightAdjustmentStatus AdjustmentStatus { get; set; }
    }
}
