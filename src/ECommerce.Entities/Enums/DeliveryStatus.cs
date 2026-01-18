// ==========================================================================
// DeliveryStatus.cs - Teslimat Görevi Durum Enum'ı
// ==========================================================================
// Bu enum, teslimat görevlerinin (DeliveryTask) yaşam döngüsündeki tüm
// olası durumları tanımlar. State machine mantığıyla çalışır ve her durum
// belirli geçişlere izin verir.
//
// Durum Geçiş Diyagramı:
// CREATED → ASSIGNED → ACCEPTED → PICKED_UP → IN_TRANSIT → DELIVERED
//                                                      ↘ FAILED
//          ↘ CANCELLED (herhangi bir aşamada iptal edilebilir)
// ==========================================================================

namespace ECommerce.Entities.Enums
{
    /// <summary>
    /// Teslimat görevi durumlarını tanımlar.
    /// Order (Sipariş) durumlarından bağımsız çalışır.
    /// DeliveryTask'ın yaşam döngüsünü yönetir.
    /// </summary>
    public enum DeliveryStatus
    {
        /// <summary>
        /// Teslimat görevi oluşturuldu, henüz kuryeye atanmadı.
        /// Bu durum, sipariş "READY_FOR_DISPATCH" olduğunda oluşur.
        /// Sonraki olası durumlar: ASSIGNED, CANCELLED
        /// </summary>
        Created = 0,

        /// <summary>
        /// Teslimat görevi bir kuryeye atandı, kurye henüz kabul etmedi.
        /// Admin manuel atama yaptığında veya otomatik atama çalıştığında bu duruma geçer.
        /// Kurye belirli süre içinde kabul etmezse otomatik reassign tetiklenir.
        /// Sonraki olası durumlar: ACCEPTED, CREATED (reassign), CANCELLED
        /// </summary>
        Assigned = 1,

        /// <summary>
        /// Kurye görevi kabul etti, henüz teslim almadı.
        /// Kurye panelinden "Kabul Et" butonu tıklandığında bu duruma geçer.
        /// Sonraki olası durumlar: PICKED_UP, CANCELLED
        /// </summary>
        Accepted = 2,

        /// <summary>
        /// Kurye paketi teslim aldı (depo/mağazadan).
        /// Kurye "Teslim Al" butonu tıkladığında bu duruma geçer.
        /// Sonraki olası durumlar: IN_TRANSIT, CANCELLED
        /// </summary>
        PickedUp = 3,

        /// <summary>
        /// Kurye yolda, müşteriye doğru hareket ediyor.
        /// Bu aşamada kurye konumu takip edilir ve ETA hesaplanır.
        /// Sonraki olası durumlar: DELIVERED, FAILED
        /// </summary>
        InTransit = 4,

        /// <summary>
        /// Teslimat başarıyla tamamlandı.
        /// Bu durum için POD (Proof of Delivery) zorunludur.
        /// Final durum - başka geçiş yok.
        /// </summary>
        Delivered = 5,

        /// <summary>
        /// Teslimat başarısız oldu.
        /// Sebep kodu ve not zorunludur (DeliveryFailure entity).
        /// Sonraki olası durumlar: ASSIGNED (yeniden atama), CANCELLED
        /// </summary>
        Failed = 6,

        /// <summary>
        /// Teslimat görevi iptal edildi.
        /// Sipariş iptali, adres sorunu veya diğer sebeplerle olabilir.
        /// Final durum - başka geçiş yok.
        /// </summary>
        Cancelled = 7
    }
}
