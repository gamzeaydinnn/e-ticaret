// ==========================================================================
// MikroDeliveryWeightsResult.cs - Mikro ERP Teslim Miktarları DTO
// ==========================================================================
// Mikro ERP'den sipariş satırlarına ait teslim miktarlarını (tartı sonuçları)
// taşıyan DTO. Sipariş hazırlanırken mağaza personelinin girdiği gerçek
// ağırlık/miktar bilgilerini e-ticaret sistemine aktarır.
//
// AKIŞ: MicroService → GetOrderDeliveryWeightsAsync → bu DTO → MikroWeightSyncService
// → OrderItem.ActualWeight güncellenir → Fark hesaplanır → Provizyon capture
// ==========================================================================

namespace ECommerce.Core.DTOs.Micro
{
    /// <summary>
    /// Mikro sipariş satırlarından çekilen teslim miktarları (tartı sonuçları).
    /// NEDEN: Mağaza personeli ürünleri tartıp Mikro'ya girdiğinde,
    /// sip_teslim_miktar alanından gerçek miktarlar çekilir.
    /// </summary>
    public class MikroDeliveryWeightsResult
    {
        /// <summary>
        /// İşlem başarılı mı?
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Hata mesajı (başarısız ise).
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// E-ticaret sipariş numarası.
        /// NEDEN: Mikro'daki sip_ozel_kod ile eşleştirme referansı.
        /// </summary>
        public string OrderNumber { get; set; } = string.Empty;

        /// <summary>
        /// Sipariş satırlarının teslim miktarları.
        /// </summary>
        public List<MikroDeliveryWeightItem> Items { get; set; } = new();
    }

    /// <summary>
    /// Tek bir sipariş satırı için Mikro'dan gelen teslim miktarı.
    /// NEDEN: Her ürün kalemi için sipariş miktarı ve gerçek tartılan
    /// miktar arasındaki fark hesaplanarak provizyon capture'a gönderilir.
    /// </summary>
    public class MikroDeliveryWeightItem
    {
        /// <summary>
        /// Stok kodu (SKU).
        /// NEDEN: OrderItem.Product.SKU ile eşleştirme için kullanılır.
        /// </summary>
        public string StokKod { get; set; } = string.Empty;

        /// <summary>
        /// Ürün adı (debug/log için).
        /// </summary>
        public string? StokIsim { get; set; }

        /// <summary>
        /// Sipariş miktarı (Mikro'daki sip_miktar).
        /// Ağırlık bazlı ürünlerde KG cinsinden gelir.
        /// </summary>
        public decimal SiparisMiktar { get; set; }

        /// <summary>
        /// Teslim miktarı (Mikro'daki sip_teslim_miktar).
        /// NEDEN: Mağaza personelinin tartıp girdiği gerçek ağırlık/miktar.
        /// Örn: Müşteri 1 KG domates sipariş etti, tartıda 1.1 KG geldi.
        /// </summary>
        public decimal TeslimMiktar { get; set; }

        /// <summary>
        /// Birim fiyat (KDV hariç).
        /// NEDEN: Fark tutarı hesaplaması için: Fark x BirimFiyat = Ek/İade tutar.
        /// </summary>
        public decimal BirimFiyat { get; set; }

        /// <summary>
        /// Teslim - Sipariş farkı (computed).
        /// Pozitif: Fazla verildi (ek tahsilat). Negatif: Eksik verildi (iade).
        /// </summary>
        public decimal Fark => TeslimMiktar - SiparisMiktar;
    }
}
