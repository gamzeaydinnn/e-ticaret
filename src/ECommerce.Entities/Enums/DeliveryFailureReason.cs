// ==========================================================================
// DeliveryFailureReason.cs - Teslimat Başarısızlık Sebep Kodları
// ==========================================================================
// Bu enum, teslimat başarısız olduğunda kuryenin seçeceği standart
// sebep kodlarını tanımlar. Raporlama ve analiz için kritik öneme sahiptir.
// ==========================================================================

namespace ECommerce.Entities.Enums
{
    /// <summary>
    /// Teslimat başarısızlık sebep kodları.
    /// Kurye, başarısız teslimat bildirirken bu kodlardan birini seçmek zorundadır.
    /// Bu kodlar analitik raporlama ve operasyonel iyileştirme için kullanılır.
    /// </summary>
    public enum DeliveryFailureReason
    {
        /// <summary>
        /// Müşteri teslimat adresinde bulunamadı.
        /// Genellikle yeniden deneme planlanır.
        /// </summary>
        CustomerNotAvailable = 0,

        /// <summary>
        /// Teslimat adresi yanlış veya eksik.
        /// Admin müdahalesi gerektirir - adres güncellenmeli.
        /// </summary>
        WrongAddress = 1,

        /// <summary>
        /// Müşteriye telefon ile ulaşılamadı.
        /// Birden fazla arama denemesi sonrası işaretlenir.
        /// </summary>
        CustomerUnreachable = 2,

        /// <summary>
        /// İş yeri/konut kapalı.
        /// Mesai saatleri dışında teslimat denendiğinde kullanılır.
        /// </summary>
        LocationClosed = 3,

        /// <summary>
        /// Müşteri teslimatı reddetti.
        /// Ürün hasarlı, yanlış ürün veya fikir değişikliği gibi sebeplerle.
        /// </summary>
        CustomerRefused = 4,

        /// <summary>
        /// Güvenlik/erişim sorunu.
        /// Site güvenliği izin vermedi, kapı kodu yanlış vb.
        /// </summary>
        AccessDenied = 5,

        /// <summary>
        /// Hava koşulları teslimatı engelledi.
        /// Sel, fırtına, kar gibi aşırı hava koşulları.
        /// </summary>
        WeatherConditions = 6,

        /// <summary>
        /// Araç arızası.
        /// Kurye aracı bozuldu, teslimat tamamlanamadı.
        /// </summary>
        VehicleBreakdown = 7,

        /// <summary>
        /// Ürün hasarlı bulundu.
        /// Teslimat öncesi hasar tespit edildi.
        /// </summary>
        PackageDamaged = 8,

        /// <summary>
        /// Kapıda ödeme (COD) yapılamadı.
        /// Müşteride nakit yok veya ödeme reddedildi.
        /// </summary>
        PaymentIssue = 9,

        /// <summary>
        /// Diğer sebepler.
        /// Not alanında detay açıklama zorunludur.
        /// </summary>
        Other = 99
    }
}
