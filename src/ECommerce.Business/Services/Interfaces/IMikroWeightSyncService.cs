// ==========================================================================
// IMikroWeightSyncService.cs - Mikro Tartı Senkronizasyon Servis Arayüzü
// ==========================================================================
// Mikro ERP'den sipariş teslim miktarlarını çekip OrderItem'lara
// senkronize eden servis arayüzü.
//
// NEDEN: Mağaza personeli ürünleri tartıp Mikro'ya girdiğinde,
// bu servis sip_teslim_miktar verilerini çekerek OrderItem.ActualWeight'i
// günceller. Böylece kurye "Teslim Ettim" dediğinde fark hesaplanır.
// ==========================================================================

namespace ECommerce.Business.Services.Interfaces
{
    /// <summary>
    /// Mikro ERP'den sipariş teslim miktarlarını çekip OrderItem'lara senkronize eder.
    /// </summary>
    public interface IMikroWeightSyncService
    {
        /// <summary>
        /// Belirtilen sipariş için Mikro'dan güncel teslim miktarlarını çeker
        /// ve OrderItem.ActualWeight alanlarını günceller.
        /// </summary>
        /// <param name="orderId">Sipariş ID</param>
        /// <param name="ct">İptal token</param>
        /// <returns>En az bir item güncellendiyse true, aksi halde false</returns>
        Task<bool> SyncDeliveryWeightsForOrderAsync(int orderId, CancellationToken ct = default);
    }
}
