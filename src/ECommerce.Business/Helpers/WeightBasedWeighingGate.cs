using System.Linq;
using ECommerce.Entities.Concrete;
using ECommerce.Entities.Enums;

namespace ECommerce.Business.Helpers
{
    /// <summary>
    /// KG ürünlerde manuel tartı ve teslimat kapıları için tek doğruluk kaynağı.
    ///
    /// İş kuralı: Tartı yalnızca Preparing'de yapılır.
    /// Sipariş onayı Admin'den; banka Capt kurye teslimatında.
    /// NEDEN Ready/Pickup kapısı: tartılmamış kg sipariş teslimata çıkarsa
    /// Capt tahmini tutarı çeker; müşteri fazla/eksik ödemiş olur.
    /// </summary>
    public static class WeightBasedWeighingGate
    {
        public const string DeniedMessage =
            "Manuel tartı yalnızca hazırlanma (Preparing) aşamasında yapılabilir. " +
            "Önce siparişi onaylayıp hazırlamaya başlayın.";

        public const string UnweighedReadyMessage =
            "Kg ürünler tartılmadan sipariş hazır olarak işaretlenemez. " +
            "Önce tüm ağırlık bazlı kalemleri tartın.";

        public const string UnweighedPickupMessage =
            "Kg ürünler tartılmadan kurye siparişi teslim alamaz / yola çıkamaz. " +
            "Market görevlisinin tartımı tamamlanmalıdır.";

        public const string StoreDeliveryDeniedMessage =
            "Kg kart siparişlerinde tartı farkı/iade kontrolü kurye teslimatında kapatılır. " +
            "Market görevlisi 'Teslim Edildi' kullanamaz.";

        /// <summary>
        /// Mağaza/admin manuel tartı (manual-weight) için izin verilen durumlar.
        /// </summary>
        public static bool CanEnterManualWeight(OrderStatus status)
            => status == OrderStatus.Preparing;

        /// <summary>
        /// Ağırlık raporları listesinde tartı bekleyen sipariş filtresi.
        /// </summary>
        public static bool IsEligibleForWeightReportsList(OrderStatus status)
            => status == OrderStatus.Preparing;

        /// <summary>
        /// Direkt satış (Sale) mimarisinde siparişler tartı kapısına takılmadan doğrudan ilerler.
        /// </summary>
        public static bool HasUnweighedWeightItems(Order? order)
            => false;

        public static bool CanMarkReady(Order? order)
            => true;

        public static bool CanCourierPickup(Order? order)
            => true;

        /// <summary>
        /// Kapıda nakit kontrolü.
        /// </summary>
        public static bool IsCashOnDelivery(Order? order)
        {
            var method = order?.PaymentMethod?.Trim().ToLowerInvariant() ?? string.Empty;
            return method is "cash" or "nakit" or "cash_on_delivery"
                or "kapida_odeme" or "kapıda ödeme" or "cod";
        }

        public static bool ShouldBlockStoreDelivery(Order? order)
            => false;
    }
}
