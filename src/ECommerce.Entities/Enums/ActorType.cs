// ==========================================================================
// ActorType.cs - Aksiyon Gerçekleştiren Aktör Tipi
// ==========================================================================
// Bu enum, bir aksiyonu kimin gerçekleştirdiğini tanımlar.
// Audit log ve event tracking için kullanılır.
// ==========================================================================

namespace ECommerce.Entities.Enums
{
    /// <summary>
    /// Sistemde aksiyon gerçekleştiren aktör tipleri.
    /// Audit log'larda "kim yaptı" sorusunun cevabı için kullanılır.
    /// </summary>
    public enum ActorType
    {
        /// <summary>
        /// Sistem tarafından otomatik yapıldı.
        /// Örn: Otomatik atama, timeout, scheduled job.
        /// </summary>
        System = 0,

        /// <summary>
        /// Admin kullanıcısı tarafından yapıldı.
        /// Admin panelden gerçekleştirilen işlemler.
        /// </summary>
        Admin = 1,

        /// <summary>
        /// Kurye tarafından yapıldı.
        /// Kurye panelinden gerçekleştirilen işlemler.
        /// </summary>
        Courier = 2,

        /// <summary>
        /// Müşteri tarafından yapıldı.
        /// Müşteri paneli veya mobil uygulamadan yapılan işlemler.
        /// </summary>
        Customer = 3,

        /// <summary>
        /// API entegrasyonu tarafından yapıldı.
        /// Dış sistemlerden gelen webhook veya API çağrıları.
        /// </summary>
        ApiIntegration = 4
    }
}
