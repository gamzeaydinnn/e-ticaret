using ECommerce.Entities.Enums;

namespace ECommerce.Business.Helpers
{
    /// <summary>
    /// KG ürünlerde manuel tartı girişinin hangi sipariş durumunda açılabileceğini
    /// tek noktadan tanımlar.
    ///
    /// İş kuralı: Tartı yalnızca hazırlanma (Preparing) aşamasında yapılır.
    /// Sipariş onayı Admin sipariş ekranından; banka kesin çekimi teslimatta Capt ile yapılır.
    /// </summary>
    public static class WeightBasedWeighingGate
    {
        public const string DeniedMessage =
            "Manuel tartı yalnızca hazırlanma (Preparing) aşamasında yapılabilir. " +
            "Önce siparişi onaylayıp hazırlamaya başlayın.";

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
    }
}
