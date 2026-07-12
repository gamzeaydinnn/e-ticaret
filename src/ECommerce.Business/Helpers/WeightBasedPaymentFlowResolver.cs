using ECommerce.Entities.Concrete;
using ECommerce.Entities.Enums;

namespace ECommerce.Business.Helpers
{
    /// <summary>
    /// KG siparişlerde ödeme akışını tek noktadan çözer: Auth→Capt veya Sale (anında çekim).
    /// </summary>
    public enum WeightBasedPaymentFlowType
    {
        NotApplicable = 0,
        /// <summary>3DS Auth — tartı sonrası Capt ile fark çekilebilir (≤ Auth×1.20).</summary>
        AuthCapture = 1,
        /// <summary>3DS Sale — checkout'ta tam çekim; tartı farkı bankadan otomatik çekilemez.</summary>
        SaleImmediate = 2
    }

    public static class WeightBasedPaymentFlowResolver
    {
        public static WeightBasedPaymentFlowType Resolve(Order? order)
        {
            if (order == null || !order.HasWeightBasedItems)
            {
                return WeightBasedPaymentFlowType.NotApplicable;
            }

            if (IsAuthCaptureFlow(order))
            {
                return WeightBasedPaymentFlowType.AuthCapture;
            }

            return WeightBasedPaymentFlowType.SaleImmediate;
        }

        public static bool CanBankCaptureOverage(Order? order)
            => Resolve(order) == WeightBasedPaymentFlowType.AuthCapture;

        /// <summary>
        /// Sale modunda checkout'ta çekilen tutar (tartı farkı karşılaştırması için).
        /// </summary>
        public static decimal ResolveSaleCapturedAmount(Order order)
        {
            if (order.CapturedAmount > 0m)
            {
                return order.CapturedAmount;
            }

            if (order.FinalPrice > 0m)
            {
                return order.FinalPrice;
            }

            return order.TotalPrice;
        }

        private static bool IsAuthCaptureFlow(Order order)
        {
            if (order.PaymentStatus == PaymentStatus.Authorized &&
                order.CaptureStatus == CaptureStatus.Pending &&
                !string.IsNullOrWhiteSpace(order.PreAuthHostLogKey))
            {
                return true;
            }

            if (order.Status == OrderStatus.PreAuthorized &&
                order.PaymentStatus == PaymentStatus.Authorized)
            {
                return true;
            }

            return order.AuthorizedAmount > 0m &&
                order.CaptureStatus == CaptureStatus.Pending &&
                !string.IsNullOrWhiteSpace(order.PreAuthHostLogKey);
        }
    }
}
