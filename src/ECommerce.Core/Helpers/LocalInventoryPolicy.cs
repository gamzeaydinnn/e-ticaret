using ECommerce.Entities.Enums;

namespace ECommerce.Core.Helpers
{
    /// <summary>
    /// Yerel stok + Mikro ToERP mimari kararları (Faz 0–4 kilit).
    ///
    /// ToERP hibrit:
    /// - Online satış/iade → belge (SiparisKaydetV2 / iade faturası) primary
    /// - Manuel/POS düzeltme → DahiliStokHareketKaydetV2 (delta push)
    ///
    /// Yerel ledger:
    /// - Master: Product.StockQuantity (+ varyant/Stocks ikincil hizalama)
    /// - Checkout: Reserve; Commit ödeme başarısında (COD: checkout'ta)
    /// - Ödeme fail: yalnızca Release (commit yoksa Restore yok)
    ///
    /// Faz 4: Mikro tracking + inbound çatışma koruması
    /// </summary>
    public static class LocalInventoryPolicy
    {
        public const string LogActionCommit = "Commit";
        public const string LogActionOrderCancelled = "OrderCancelled";
        public const string LogActionRefund = "Refund";
        public const string LogActionPaymentFailed = "PaymentFailed";
        public const string LogActionReleaseUnpaid = "ReleaseUnpaid";

        public const string MikroTrackingPrefix = "MIKRO-";

        /// <summary>
        /// Sipariş restore sonrası mutlak stok push kapalı — iade Mikro'ya belge ile gider.
        /// </summary>
        public const bool PushOutboundOnOrderStockRestore = false;

        /// <summary>
        /// Online satış: SiparisKaydetV2 (sipariş belgesi) + DahiliStokHareketKaydetV2 (eldeki stok düşüşü).
        /// Sipariş tek başına Mikro'da eldeki miktarı düşürmez; stok çıkışı dahili harekettedir.
        /// </summary>
        public const bool UseSiparisDocumentForOnlineSaleStock = true;

        /// <summary>
        /// Satış sonrası Mikro eldeki stoğu DahiliStokHareket ile düşür.
        /// </summary>
        public const bool PushDahiliStockDecreaseOnOnlineSale = true;

        /// <summary>
        /// İade sonrası Mikro eldeki stoğu DahiliStokHareket ile artır
        /// (iade faturası başarısız olsa bile stok geri gelsin).
        /// </summary>
        public const bool PushDahiliStockIncreaseOnOnlineRefund = true;

        public const string SyncEntityOrderStockDecrease = "OrderStockDecrease";
        public const string SyncEntityOrderStockIncrease = "OrderStockIncrease";
        public const string SyncEntityRefundInvoice = "RefundInvoice";

        public const int PaymentInFlightReservationExtraMinutes = 45;

        public static bool IsCashOnDelivery(string? paymentMethod)
        {
            if (string.IsNullOrWhiteSpace(paymentMethod))
            {
                return false;
            }

            var normalized = paymentMethod.Trim().ToLowerInvariant()
                .Replace("-", "_")
                .Replace(" ", "_");

            return normalized is "cash_on_delivery" or "cod" or "kapida_odeme" or "kapıda_ödeme";
        }

        public static bool IsMikroSyncedTracking(string? trackingNumber)
        {
            return !string.IsNullOrWhiteSpace(trackingNumber) &&
                   trackingNumber.StartsWith(MikroTrackingPrefix, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// FromERP stok artışı: Mikro henüz online satışı görmemişse web stokunu yükseltme.
        /// </summary>
        public static bool ShouldSkipInboundStockIncrease(
            int localQuantity,
            int mikroQuantity,
            bool hasPendingUnsyncedOnlineSale)
        {
            return hasPendingUnsyncedOnlineSale && mikroQuantity > localQuantity;
        }

        public static bool HoldsCommittedSellableStock(OrderStatus status, bool isInventoryCommitted)
        {
            if (isInventoryCommitted)
            {
                return status is not (OrderStatus.Cancelled or OrderStatus.Refunded);
            }

            return false;
        }

        public static bool HoldsCommittedSellableStock(OrderStatus status)
        {
            return status is not (
                OrderStatus.Cancelled or
                OrderStatus.Refunded or
                OrderStatus.PaymentFailed or
                OrderStatus.Pending or
                OrderStatus.New);
        }

        public static bool IsPaymentSuccessStatus(OrderStatus status)
        {
            return status is
                OrderStatus.Paid or
                OrderStatus.PreAuthorized or
                OrderStatus.Confirmed or
                OrderStatus.Preparing or
                OrderStatus.Processing or
                OrderStatus.Ready or
                OrderStatus.ReadyForPickup or
                OrderStatus.Assigned or
                OrderStatus.WeightPending;
        }
    }
}
