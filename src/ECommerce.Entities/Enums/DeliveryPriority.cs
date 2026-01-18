// ==========================================================================
// DeliveryPriority.cs - Teslimat Öncelik Seviyeleri
// ==========================================================================
// Bu enum, teslimat görevlerinin öncelik seviyelerini tanımlar.
// Kurye atama algoritması ve görev sıralamasında kullanılır.
// ==========================================================================

namespace ECommerce.Entities.Enums
{
    /// <summary>
    /// Teslimat görevi öncelik seviyeleri.
    /// Daha yüksek öncelik = Daha önce atama ve teslimat.
    /// </summary>
    public enum DeliveryPriority
    {
        /// <summary>
        /// Düşük öncelik.
        /// Teslimat zaman penceresi geniş, aciliyet yok.
        /// </summary>
        Low = 0,

        /// <summary>
        /// Normal öncelik.
        /// Standart teslimat, varsayılan değer.
        /// </summary>
        Normal = 1,

        /// <summary>
        /// Yüksek öncelik.
        /// Müşteri talebine göre veya SLA riski olan görevler.
        /// </summary>
        High = 2,

        /// <summary>
        /// Acil teslimat.
        /// Express teslimat, VIP müşteri veya kritik sipariş.
        /// En önce atanır ve teslim edilir.
        /// </summary>
        Urgent = 3,

        /// <summary>
        /// Kritik öncelik.
        /// Sistem tarafından otomatik belirlenir.
        /// SLA ihlali riski veya müşteri şikayeti durumunda.
        /// </summary>
        Critical = 4
    }
}
